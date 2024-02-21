using Application.Helpers;
using Application.Mappers;
using Application.Services;
using SharedData.Data.Enums;
using SharedData.Data.Models;
using SharedData.Models;

public class Program
{
    static void Main()
    {
        // Specify the path to dataset CSV files
        string csvFile = CsvServiceHelper.GetCsvFilePath("SharedData", "Data", "Csv", "TaxiRouteData-October.csv");
        string csvFile1 = CsvServiceHelper.GetCsvFilePath("SharedData", "Data", "Csv", "TaxiRouteData-October1.csv");

        //Read csv data and create a list from them
        Console.WriteLine("First csv file - Car 1");
        List<TaxiRoutePositionModel> taxiRoutePositionsDataList = CsvServiceHelper.ReadCsvWithProgress<TaxiRoutePositionModel, TaxiRoutePositionModelMap>(csvFile, progress => Console.WriteLine($"Reading data: {progress}%"));

        Console.WriteLine("Second csv file - Car 2");
        List<TaxiRoutePositionModel> taxiRoutePositionsDataList1 = CsvServiceHelper.ReadCsvWithProgress<TaxiRoutePositionModel, TaxiRoutePositionModelMap>(csvFile1, progress => Console.WriteLine($"Reading data: {progress}%"));
                
        taxiRoutePositionsDataList.AddRange(taxiRoutePositionsDataList1);

        // Clean dataset - get only data between 15:00 and 18:00 for each day
        taxiRoutePositionsDataList = taxiRoutePositionsDataList.Where(item => item.IsActive && item.Time.Hour >= 15 && item.Time.Hour <= 18).ToList();

        RouteService routeService = new RouteService();

        var testList = routeService.GetAllRouteAddressAndId(taxiRoutePositionsDataList);

        var roadSegments = routeService.GetRoadSegments(testList);

        var segments = roadSegments.SelectMany(x => x.Select(x => new { x.Name, x.StartNode, x.EndNode, x.StartNodeNumber, x.EndNodeNumber })).ToList();

        List<VisitedSegement> allvisitedSegmentsAndInfo = new List<VisitedSegement>();
        string addedSegment = "";

        int routeCounter = 0;
        int rightDir = 0;
        int leftDir = 0;
        int straightDir = 0;
        int backDir = 0;

        for (int i = 0; i < taxiRoutePositionsDataList.Count; i++)
        {

            if (taxiRoutePositionsDataList[i].HasPassenger)
            {
                var visitedSegment = segments.Where(x => Math.Min(x.StartNode.Lat, x.EndNode.Lat) <= taxiRoutePositionsDataList[i].Latitude
                                                   && Math.Max(x.StartNode.Lat, x.EndNode.Lat) >= taxiRoutePositionsDataList[i].Latitude
                                                   && Math.Min(x.StartNode.Lon, x.EndNode.Lon) <= taxiRoutePositionsDataList[i].Longitute
                                                   && Math.Max(x.StartNode.Lon, x.EndNode.Lon) >= taxiRoutePositionsDataList[i].Longitute).FirstOrDefault();

                if(i < taxiRoutePositionsDataList.Count - 1)
                {
                    if(taxiRoutePositionsDataList[i + 1].HasPassenger)
                    {
                        // Calculate bearing
                        double bearing = MovementAnalyzer.CalculateBearing(taxiRoutePositionsDataList[i].Longitute, taxiRoutePositionsDataList[i].Latitude, taxiRoutePositionsDataList[i + 1].Longitute, taxiRoutePositionsDataList[i + 1].Latitude);

                        // Determine direction
                        Direction direction = MovementAnalyzer.DetermineDirection(bearing);

                        switch(direction)
                        {
                            case Direction.Right:
                                rightDir++;
                                break;
                            case Direction.Left: 
                                leftDir++; 
                                break;
                            case Direction.Straight:
                                straightDir++;
                                break;
                            default:
                                backDir++;
                                break;
                        }
                    }
                }

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
                            Time = taxiRoutePositionsDataList[i].Time,
                            RouteCounter = routeCounter,
                            RightDirection = rightDir,
                            LeftDirection = leftDir,
                            StraightDirection = straightDir,
                            BackDirection = backDir
                        });

                        addedSegment = visitedSegment.Name;
                    }
                    else
                    {
                        if(visitedSegment.Name == addedSegment)
                        {
                            TimeSpan timeDiff = taxiRoutePositionsDataList[i].Time - allvisitedSegmentsAndInfo.Last().Time;

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
                                Time = taxiRoutePositionsDataList[i].Time,
                                RouteCounter = routeCounter,
                                RightDirection = rightDir,
                                LeftDirection = leftDir,
                                StraightDirection = straightDir,
                                BackDirection = backDir
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

            rightDir = 0;
            leftDir = 0;
            straightDir = 0;
            backDir = 0;
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
                                                 AverageTimeAmount = group.Average(seg => seg.TimeAmount),
                                                 RightDirection = group.Sum(seg => seg.RightDirection),
                                                 LeftDirection = group.Sum(seg => seg.LeftDirection),
                                                 StraightDirection = group.Sum(seg => seg.StraightDirection),
                                                 BackDirection = group.Sum(seg => seg.BackDirection),
                                             });

        Console.WriteLine($"{groupedVisitedSegments.Select(x => x.AverageTimeAmount).Sum().ToString("F0")} {groupedVisitedSegments.Select(x => x.Name).Count() + 2} {groupedVisitedSegments.Select(x => x.Name).Count()} {allvisitedSegmentsAndInfo.GroupBy(x => x.RouteCounter).Count()} 100");

        finalVisitedSegments.Add($"{groupedVisitedSegments.Select(x => x.AverageTimeAmount).Sum().ToString("F0")} {groupedVisitedSegments.Select(x => x.Name).Count() + 2} {groupedVisitedSegments.Select(x => x.Name).Count()} {allvisitedSegmentsAndInfo.GroupBy(x => x.RouteCounter).Count()} 100\n");

        foreach (var visitedSeg in groupedVisitedSegments)
        {
            Console.WriteLine($"{visitedSeg.StartNodeNumber} {visitedSeg.EndNodeNumber} {visitedSeg.Name} {visitedSeg.AverageTimeAmount.ToString("F0")} {visitedSeg.BackDirection} {visitedSeg.LeftDirection} {visitedSeg.StraightDirection} {visitedSeg.RightDirection}");
            finalVisitedSegments.Add($"{visitedSeg.StartNodeNumber} {visitedSeg.EndNodeNumber} {visitedSeg.Name} {visitedSeg.AverageTimeAmount.ToString("F0")} {visitedSeg.BackDirection} {visitedSeg.LeftDirection} {visitedSeg.StraightDirection} {visitedSeg.RightDirection}");
        }

        finalVisitedSegments.Add("\n");

        foreach(var completedRoute in allvisitedSegmentsAndInfo.GroupBy(x => x.RouteCounter))
        {
            Console.WriteLine($"{completedRoute.Count()} {string.Join(" ", completedRoute.Select(x => x.Name))}");
            finalVisitedSegments.Add($"{completedRoute.Count()} {string.Join(" ", completedRoute.Select(x => x.Name))}");
        }

        //Save new text file with all the addresses visited in every passenger route
        string allVisitedAddressesPerPassengerTxtFile = TxtServiceHelper.GetTxtFilePath("SharedData", "Data", "Txt", "VisitedSegmentsPerPassenger-October.txt");
        TxtServiceHelper.WriteListToTxt(finalVisitedSegments, allVisitedAddressesPerPassengerTxtFile);

        Console.WriteLine("All visited segments per passenger written in the text file successfully!");
    }
}