public static class AgentParsing
{
    public static Result<Agent> ParseAgent(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return FormatError("line is empty");

        string[] parts = line.Split(';');
        if (parts.Length != 2)
            return FormatError("expected 2 fields: name;capacity");
        if (!int.TryParse(parts[1].Trim(), out int capacity))
            return FormatError("capacity must be an integer");

        return Agent.Create(parts[0], capacity);
    }

    public static Result<Agent[]> ParseAgents(string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var agents = new List<Agent>();
        var problems = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            switch (ParseAgent(lines[i]))
            {
                case Result<Agent>.Ok ok:
                    agents.Add(ok.Value);
                    break;
                case Result<Agent>.Fail fail:
                    problems.Add($"line {i + 1}: {fail.Error.Message}");
                    break;
            }
        }

        if (problems.Count > 0)
            return new Result<Agent[]>.Fail(new Error(ErrorCode.DataMalformed, string.Join("; ", problems)));

        bool unique = agents.Select(a => a.Name.ToUpperInvariant()).Distinct().Count() == agents.Count;
        if (!unique)
            return new Result<Agent[]>.Fail(new Error(ErrorCode.DuplicateAgent, "agent names must be unique"));

        return new Result<Agent[]>.Ok(agents.ToArray());
    }

    private static Result<Agent> FormatError(string message)
        => new Result<Agent>.Fail(new Error(ErrorCode.AgentFormat, message));
}

public static class AgentFile
{
    public static Result<Agent[]> Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        Result<string[]> read = LineFile.ReadAll(path);
        if (read is Result<string[]>.Fail readFailed)
            return new Result<Agent[]>.Fail(readFailed.Error);
        string[] lines = ((Result<string[]>.Ok)read).Value;

        Result<Agent[]> parsed = AgentParsing.ParseAgents(lines);
        if (parsed is Result<Agent[]>.Fail rejected)
            return new Result<Agent[]>.Fail(new Error(rejected.Error.Code, $"{path}: {rejected.Error.Message}"));

        return parsed;
    }
}
