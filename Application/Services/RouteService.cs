using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedData.Data.Models;
using SharedData.Models;
using System.Text;

namespace Application.Services;

public class RouteService
{
    private const string HttpHeaderUserAgent = "User-Agent";
    private const string ProjectName = "TaxiRouteTracker";
    private const string NominationOpenStreetMapApiUrl = "https://nominatim.openstreetmap.org/reverse?format=json&lat={0}&lon={1}";
    private const string NominationOpenStreetMapNodeApiUrl = "https://overpass-api.de/api/interpreter?data=[out:json];way({0});(._;%3E;);out;";

    public string GetAddress(double latitude, double longitude)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add(HttpHeaderUserAgent, ProjectName);

            string apiUrl = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}";

            HttpResponseMessage response = client.GetAsync(apiUrl).Result;

            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;
                if (!string.IsNullOrEmpty(result))
                {
                    return GetStreetName(result);
                }
            }

            return "";
        }
    }

    public List<string> GetAllRouteAddresses(List<TaxiRoutePositionModel> allTaxiRoutes, Action<double> progressCallback)
    {
        List<string> allPassengerRoutes = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add(HttpHeaderUserAgent, ProjectName);

            StringBuilder apiUrl = new StringBuilder();
            StringBuilder passengerRoute = new StringBuilder();
            StringBuilder lastAddressAdded = new StringBuilder();
            StringBuilder newAddressToBeAdded = new StringBuilder();

            int commonCounter = 0;
            int totalRecords = allTaxiRoutes.Count;
            int processedRecords = 0;

            // Time complexity - O(n) where n is the number of records in the taxi device dataset. Space complexity - O(1)
            foreach (var position in allTaxiRoutes)
            {
                if (position.HasPassenger)
                {
                    apiUrl.AppendFormat(NominationOpenStreetMapApiUrl, position.Latitude, position.Longitute);

                    HttpResponseMessage response = client.GetAsync(apiUrl.ToString()).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        if (!string.IsNullOrEmpty(result))
                        {
                            newAddressToBeAdded.Append(GetStreetName(result));

                            if (commonCounter > 0)
                            {
                                if(newAddressToBeAdded.ToString() != lastAddressAdded.ToString())
                                {
                                    passengerRoute.AppendFormat(", {0}", newAddressToBeAdded.ToString());

                                    lastAddressAdded.Clear();
                                    lastAddressAdded.Append(newAddressToBeAdded.ToString());
                                }
                            }
                            else
                            {
                                passengerRoute.Append(newAddressToBeAdded.ToString());

                                lastAddressAdded.Append(newAddressToBeAdded.ToString());
                            }

                            newAddressToBeAdded.Clear();
                            commonCounter++;
                        }
                    }

                    apiUrl.Clear();
                }
                else
                {
                    if(passengerRoute.Length > 0)
                    {
                        passengerRoute.Append(".");
                        allPassengerRoutes.Add(passengerRoute.ToString());
                        passengerRoute.Clear();
                    }

                    newAddressToBeAdded.Clear();
                    lastAddressAdded.Clear();
                    commonCounter = 0;
                }

                processedRecords++;
                double progress = ((double)processedRecords / totalRecords) * 100;
                progress = Math.Min(progress, 100); // Ensure progress is capped at 100%
                progressCallback?.Invoke(progress);
            }

            if(passengerRoute.Length > 0)
            {
                passengerRoute.Append(".");
                allPassengerRoutes.Add(passengerRoute.ToString());
                passengerRoute.Clear();
            }
        }

        return allPassengerRoutes;
    }

    public List<RoadLocation> GetAllRouteAddressAndId(List<TaxiRoutePositionModel> allTaxiRoutes)
    {
        List<RoadLocation> allPassengerRoutes = new List<RoadLocation>();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add(HttpHeaderUserAgent, ProjectName);

            StringBuilder apiUrl = new StringBuilder();
            StringBuilder passengerRoute = new StringBuilder();
            StringBuilder lastAddressAdded = new StringBuilder();
            StringBuilder newAddressToBeAdded = new StringBuilder();

            int totalRecords = allTaxiRoutes.Count;

            // Time complexity - O(n) where n is the number of records in the taxi device dataset. Space complexity - O(1)
            foreach (var position in allTaxiRoutes)
            {
                if (position.HasPassenger)
                {
                    apiUrl.AppendFormat(NominationOpenStreetMapApiUrl, position.Latitude, position.Longitute);

                    HttpResponseMessage response = client.GetAsync(apiUrl.ToString()).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;

                        var road = GetRoadNameAndId(result);

                        if(road.Name != "N/A")
                        {
                            allPassengerRoutes.Add(GetRoadNameAndId(result));
                        }
                    }

                    apiUrl.Clear();
                }
            }

        }

        return allPassengerRoutes.DistinctBy(x => x.Name).ToList();
    }

    public List<List<RoadSegment>> GetRoadSegments(List<RoadLocation> roads)
    {
        List<List<RoadSegment>> roadSegments = new List<List<RoadSegment>>();

        StringBuilder apiUrl = new StringBuilder();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add(HttpHeaderUserAgent, ProjectName);

            foreach(var road in roads)
            {
                apiUrl.AppendFormat(NominationOpenStreetMapNodeApiUrl, road.Id);

                HttpResponseMessage response = client.GetAsync(apiUrl.ToString()).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    roadSegments.Add(GetRoadSegmentNameAndId(result, road.Name));
                }

                apiUrl.Clear();
            }

        }

        return roadSegments;
    }

    private static string GetStreetName(string jsonResponse)
    {
        JObject jsonObject = JObject.Parse(jsonResponse);

        string streetName = (string)(jsonObject["address"]!["road"] ?? "N/A")!;

        return streetName.Replace(" ", "_");
    }


    private static RoadLocation GetRoadNameAndId(string jsonResponse)
    {
        JObject jsonObject = JObject.Parse(jsonResponse);

        RoadLocation roadLoc = new RoadLocation();

        roadLoc.Id = (string)jsonObject["osm_id"];

        var streetName = (string)(jsonObject["address"]!["road"] ?? "N/A")!;

        roadLoc.Name =  streetName.Replace(" ", "_");

        return roadLoc;
    }

    private static List<RoadSegment> GetRoadSegmentNameAndId(string jsonResponse, string roadName)
    {
        List<RoadSegment> segments = new List<RoadSegment>();

        NodesElement elements = new NodesElement();

        elements = JsonConvert.DeserializeObject<NodesElement>(jsonResponse);

        for(int i = 0; i < elements.Elements.Count - 1; i++)
        {
            RoadSegment seg = new RoadSegment();

            seg.Name = $"{roadName}_{i}_{i + 1}";
            seg.StartNode = elements.Elements[i];    
            seg.EndNode = elements.Elements[i + 1];
            seg.StartNodeNumber = i;
            seg.EndNodeNumber = i + 1;

            segments.Add(seg);
        }

        return segments;
    }
}
