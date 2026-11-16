public static class ReportFormat
{
    public static string[] ToCsvLines(QueueReport report)
    {
        var lines = new List<string> { "rank;id;title;score;cost_cents" };
        foreach (PricedRow row in report.Priority)
            lines.Add($"{row.Rank};{row.Id};{row.Title};{row.Score};{row.CostCents}");
        lines.Add($"TOTAL;;;;{report.TotalCostCents}");
        return lines.ToArray();
    }
}

public static class ReportFile
{
    public static Result<string> Export(string path, IEnumerable<string> lines)
    {
        string tempPath = path + ".tmp";
        try
        {
            using (var writer = new StreamWriter(tempPath))
            {
                foreach (string line in lines)
                    writer.WriteLine(line);
            }
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                   || ex is ArgumentException || ex is NotSupportedException)
        {
            DeleteQuietly(tempPath);
            return new Result<string>.Fail(new Error(ErrorCode.FileWriteFailed, $"cannot write report {path}: {ex.Message}"));
        }
        return new Result<string>.Ok(path);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                   || ex is ArgumentException || ex is NotSupportedException)
        {
        }
    }
}
