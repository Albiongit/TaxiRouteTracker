using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;

namespace Application.Helpers;

public static class CsvServiceHelper
{
    public static List<T> ReadCsvWithProgress<T, TMap>(string filePath, Action<double> progressCallback) where TMap : ClassMap<T>
    {
        List<T> resultList = new List<T>();

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.Context.RegisterClassMap<TMap>();

            // Read all records and store them in a list
            resultList = csv.GetRecords<T>().ToList();

            int totalRecords = resultList.Count;
            int processedRecords = 0;

            // Process records and report progress
            foreach (var record in resultList)
            {
                // Process the record here

                processedRecords++;
                double progress = ((double)processedRecords / totalRecords) * 100;
                progressCallback?.Invoke(progress);
            }
        }

        return resultList;
    }




    public static string GetCsvFilePath(params string[] paths)
    {
        string solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;

        return Path.GetFullPath(Path.Combine(solutionDirectory, Path.Combine(paths)));
    }
}
