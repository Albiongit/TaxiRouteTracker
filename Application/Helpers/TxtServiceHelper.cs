namespace Application.Helpers;

public static class TxtServiceHelper
{
    public static string GetTxtFilePath(params string[] paths)
    {
        string solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;

        return Path.GetFullPath(Path.Combine(solutionDirectory, Path.Combine(paths)));
    }

    public static void WriteListToTxt(List<string> strings, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (string str in strings)
            {
                writer.WriteLine(str);
            }
        }
    }
}
