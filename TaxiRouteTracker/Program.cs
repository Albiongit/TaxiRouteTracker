using Application.Helpers;
using Application.Mappers;
using Application.Services;
using SharedData.Models;

public class Program
{
    static void Main()
    {
        // Specify the path to dataset CSV file
        string csvFile = CsvServiceHelper.GetCsvFilePath("SharedData", "Data", "Csv", "TaxiRouteData-October.csv");

        //Read csv data and create a list from them
        List<TaxiRoutePositionModel> taxiRoutePositionsDataList = CsvServiceHelper.ReadCsv<TaxiRoutePositionModel, TaxiRoutePositionModelMap>(csvFile);

        // Clean dataset
        taxiRoutePositionsDataList = taxiRoutePositionsDataList.Where(item => item.IsActive).ToList();

        RouteService routeService = new RouteService();

        // Test first 1000 records
        var allVisitedAddressesPerPassengerList = routeService.GetAllRouteAddresses(taxiRoutePositionsDataList.Take(1000).ToList());

        //Save new text file with all the addresses visited in every passenger route
        string allVisitedAddressesPerPassengerTxtFile = TxtServiceHelper.GetTxtFilePath("SharedData", "Data", "Txt", "VisitedAddresesPerPassenger-October.txt");
        TxtServiceHelper.WriteListToTxt(allVisitedAddressesPerPassengerList, allVisitedAddressesPerPassengerTxtFile);

        Console.WriteLine("All visited addresses per passenger written in the text file successfully!");
    }
}