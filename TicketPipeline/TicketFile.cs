public static class TicketFile
{
    public static Result<Ticket[]> Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        Result<string[]> read = LineFile.ReadAll(path);
        if (read is Result<string[]>.Fail readFailed)
            return new Result<Ticket[]>.Fail(readFailed.Error);
        string[] lines = ((Result<string[]>.Ok)read).Value;

        Result<Ticket[]> parsed = Pipeline.ParseTickets(lines);
        if (parsed is Result<Ticket[]>.Fail rejected)
            return new Result<Ticket[]>.Fail(new Error(rejected.Error.Code, $"{path}: {rejected.Error.Message}"));

        return parsed;
    }
}
