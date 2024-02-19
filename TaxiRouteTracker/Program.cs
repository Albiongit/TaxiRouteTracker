using Application.Helpers;
using Application.Mappers;
using Application.Services;
using SharedData.Data.Models;
using SharedData.Models;

public class Program
{
    static void Main()
    {
        // Specify the path to dataset CSV file
        string csvFile = CsvServiceHelper.GetCsvFilePath("SharedData", "Data", "Csv", "TaxiRouteData-October.csv");

        //Read csv data and create a list from them
        List<TaxiRoutePositionModel> taxiRoutePositionsDataList = CsvServiceHelper.ReadCsvWithProgress<TaxiRoutePositionModel, TaxiRoutePositionModelMap>(csvFile, progress => Console.WriteLine($"Reading data: {progress}%"));


        // Clean dataset - test only 100 records
        taxiRoutePositionsDataList = taxiRoutePositionsDataList.Where(item => item.IsActive).Take(50).ToList();

        RouteService routeService = new RouteService();

        var testList = routeService.GetAllRouteAddressAndId(taxiRoutePositionsDataList);

        var roadSegments = routeService.GetRoadSegments(testList);

        var segments = roadSegments.SelectMany(x => x.Select(x => new { x.Name, x.StartNode, x.EndNode, x.StartNodeNumber, x.EndNodeNumber })).ToList();

        List<VisitedSegement> allvisitedSegmentsAndInfo = new List<VisitedSegement>();
        string addedSegment = "";

        int routeCounter = 0;

        foreach (var route in taxiRoutePositionsDataList)
        {

            if (route.HasPassenger)
            {
                var visitedSegment = segments.Where(x => Math.Min(x.StartNode.Lat, x.EndNode.Lat) <= route.Latitude
                                                   && Math.Max(x.StartNode.Lat, x.EndNode.Lat) >= route.Latitude
                                                   && Math.Min(x.StartNode.Lon, x.EndNode.Lon) <= route.Longitute
                                                   && Math.Max(x.StartNode.Lon, x.EndNode.Lon) >= route.Longitute).FirstOrDefault();

                if(visitedSegment != null)
                {
                    if (string.IsNullOrEmpty(addedSegment))
                    {
                        allvisitedSegmentsAndInfo.Add(new VisitedSegement
                        {
                            StartNodeNumber = visitedSegment.StartNodeNumber,
                            EndNodeNumber = visitedSegment.EndNodeNumber,
                            Name = visitedSegment.Name,
                            TimeAmount = 5,
                            Time = route.Time,
                            RouteCounter = routeCounter
                        });

                        addedSegment = visitedSegment.Name;
                    }
                    else
                    {
                        if(visitedSegment.Name == addedSegment)
                        {
                            TimeSpan timeDiff = route.Time - allvisitedSegmentsAndInfo.Last().Time;

                            allvisitedSegmentsAndInfo.Last().TimeAmount += (int)timeDiff.TotalSeconds;
                        }
                        else
                        {
                            addedSegment = visitedSegment.Name;

                            allvisitedSegmentsAndInfo.Add(new VisitedSegement
                            {
                                StartNodeNumber = visitedSegment.StartNodeNumber,
                                EndNodeNumber = visitedSegment.EndNodeNumber,
                                Name = visitedSegment.Name,
                                TimeAmount = 5,
                                Time = route.Time,
                                RouteCounter = routeCounter
                            });
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(addedSegment))
                {
                    addedSegment = "";
                    routeCounter++;
                }
            }
        }

        Console.WriteLine();

        List<string> finalVisitedSegments = new List<string>();

        // Grouper segments for general infos abous visisted segments with their average time
        var groupedVisitedSegments = allvisitedSegmentsAndInfo.GroupBy(seg => seg.Name)
                                             .Select(group => new
                                             {
                                                 Name = group.Key,
                                                 StartNodeNumber = group.Select(seg => seg.StartNodeNumber).First(),
                                                 EndNodeNumber = group.Select(seg => seg.EndNodeNumber).First(),
                                                 AverageTimeAmount = group.Average(seg => seg.TimeAmount)
                                             });

        Console.WriteLine($"{groupedVisitedSegments.Select(x => x.AverageTimeAmount).Sum()} {groupedVisitedSegments.Select(x => x.Name).Count() + 2} {groupedVisitedSegments.Select(x => x.Name).Count()} {allvisitedSegmentsAndInfo.GroupBy(x => x.RouteCounter).Count()} 100");

        finalVisitedSegments.Add($"{groupedVisitedSegments.Select(x => x.AverageTimeAmount).Sum()} {groupedVisitedSegments.Select(x => x.Name).Count() + 2} {groupedVisitedSegments.Select(x => x.Name).Count()} {allvisitedSegmentsAndInfo.GroupBy(x => x.RouteCounter).Count()} 100\n");

        foreach (var visitedSeg in groupedVisitedSegments)
        {
            Console.WriteLine($"{visitedSeg.StartNodeNumber} {visitedSeg.EndNodeNumber} {visitedSeg.Name} {visitedSeg.AverageTimeAmount}");
            finalVisitedSegments.Add($"{visitedSeg.StartNodeNumber} {visitedSeg.EndNodeNumber} {visitedSeg.Name} {visitedSeg.AverageTimeAmount}");
        }

        finalVisitedSegments.Add("\n");

        foreach(var completedRoute in allvisitedSegmentsAndInfo.GroupBy(x => x.RouteCounter))
        {
            Console.WriteLine($"{completedRoute.Count()} {string.Join(" ", completedRoute.Select(x => x.Name))}");
            finalVisitedSegments.Add($"{completedRoute.Count()} {string.Join(" ", completedRoute.Select(x => x.Name))}");
        }

        ////Save new text file with all the addresses visited in every passenger route
        string allVisitedAddressesPerPassengerTxtFile = TxtServiceHelper.GetTxtFilePath("SharedData", "Data", "Txt", "VisitedSegmentsPerPassenger-October.txt");
        TxtServiceHelper.WriteListToTxt(finalVisitedSegments, allVisitedAddressesPerPassengerTxtFile);

        Console.WriteLine("All visited segments per passenger written in the text file successfully!");
    }
}