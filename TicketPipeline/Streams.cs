public static class Streams
{
    public static IEnumerable<ScoredTicket> ScoreLazily(IEnumerable<Ticket> tickets, Func<Ticket, int> score)
    {
        foreach (Ticket ticket in tickets)
            yield return new ScoredTicket(ticket, score(ticket));
    }

    public static IEnumerable<string> ReadLines(Func<TextReader> open)
    {
        using TextReader reader = open();
        while (reader.ReadLine() is { } line)
            yield return line;
    }
}

public static class LineFile
{
    public static Result<string[]> ReadAll(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try
        {
            string[] lines = Streams.ReadLines(() => new StreamReader(path)).ToArray();
            return new Result<string[]>.Ok(lines);
        }
        catch (FileNotFoundException)
        {
            return Failure(ErrorCode.FileMissing, $"file not found: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            return Failure(ErrorCode.FileMissing, $"folder of the file not found: {path}");
        }

        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                   || ex is ArgumentException || ex is NotSupportedException)
        {
            return Failure(ErrorCode.FileUnreadable, $"cannot read file {path}: {ex.Message}");
        }
    }

    private static Result<string[]> Failure(string code, string message)
        => new Result<string[]>.Fail(new Error(code, message));
}
