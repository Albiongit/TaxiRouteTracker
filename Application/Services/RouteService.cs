using Newtonsoft.Json.Linq;
using SharedData.Models;
using System.Text;

namespace Application.Services;

public class RouteService
{
    private const string HttpHeaderUserAgent = "User-Agent";
    private const string ProjectName = "TaxiRouteTracker";
    private const string NominationOpenStreetMapApiUrl = "https://nominatim.openstreetmap.org/reverse?format=json&lat={0}&lon={1}";

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

    private static string GetStreetName(string jsonResponse)
    {
        JObject jsonObject = JObject.Parse(jsonResponse);

        string streetName = (string)(jsonObject["address"]!["road"] ?? jsonObject["address"]!["suburb"] ?? "N/A")!;

        return streetName;
    }
}
