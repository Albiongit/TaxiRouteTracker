using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;

namespace Application.Helpers;

public static class CsvServiceHelper
{
    public static List<T> ReadCsv<T, TMap>(string filePath) where TMap : ClassMap<T>
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.Context.RegisterClassMap<TMap>();
            return csv.GetRecords<T>().ToList();
        }
    }

    public static string GetCsvFilePath(params string[] paths)
    {
        string solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;

        return Path.GetFullPath(Path.Combine(solutionDirectory, Path.Combine(paths)));
    }
}
