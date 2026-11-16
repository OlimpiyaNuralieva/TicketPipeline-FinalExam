public static class Pipeline
{
    public static Result<Ticket> ParseTicket(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return FormatError("line is empty");

        string[] parts = line.Split(';');
        if (parts.Length != 5)
            return FormatError("expected 5 fields");

        var formatErrors = new List<string>();

        if (!int.TryParse(parts[0].Trim(), out int id))
            formatErrors.Add("id must be an integer");
        if (!int.TryParse(parts[2].Trim(), out int effort))
            formatErrors.Add("effort must be an integer");
        if (!bool.TryParse(parts[3].Trim(), out bool isOpen))
            formatErrors.Add("isOpen must be true or false");

        string kindText = parts[4].Trim();
        bool kindKnown = Enum.GetNames<TicketKind>().Contains(kindText, StringComparer.OrdinalIgnoreCase);
        if (!kindKnown)
            formatErrors.Add("kind must be Question, Request or Incident");

        if (formatErrors.Count > 0)
            return FormatError(string.Join("; ", formatErrors));

        return Ticket.Create(id, parts[1], effort, isOpen, Enum.Parse<TicketKind>(kindText, true));
    }

    public static Result<Ticket[]> ParseTickets(string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var tickets = new List<Ticket>();
        var problems = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            switch (ParseTicket(lines[i]))
            {
                case Result<Ticket>.Ok parsed:
                    tickets.Add(parsed.Value);
                    break;
                case Result<Ticket>.Fail rejected:
                    problems.Add($"line {i + 1}: {rejected.Error.Message}");
                    break;
            }
        }

        if (problems.Count > 0)
            return new Result<Ticket[]>.Fail(new Error(ErrorCode.DataMalformed, string.Join("; ", problems)));

        return new Result<Ticket[]>.Ok(tickets.ToArray());
    }

    public static Option<Ticket> FindTicket(IEnumerable<Ticket> tickets, int id)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        foreach (Ticket ticket in tickets)
        {
            if (ticket.Id == id)
                return new Option<Ticket>.Some(ticket);
        }
        return new Option<Ticket>.None();
    }

    private static Result<Ticket> FormatError(string message)
        => new Result<Ticket>.Fail(new Error(ErrorCode.TicketFormat, message));

    public static Ticket[] SelectWorkable(IEnumerable<Ticket> tickets, int maxEffort)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        return tickets.Workable(maxEffort).ToArray();
    }

    public static ScoredTicket[] ScoreTickets(IEnumerable<Ticket> tickets, Func<Ticket, int> policy)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentNullException.ThrowIfNull(policy);
        return tickets
            .Select(t => new ScoredTicket(t, policy(t)))
            .ToArray();
    }

    public static int SeverityFirst(Ticket t) => Scoring.Severity(t);

    public static int QuickWinsFirst(Ticket t) => Scoring.QuickWins(t);

    public static Recommendation[] BuildRecommendations(IEnumerable<ScoredTicket> scored)
    {
        ArgumentNullException.ThrowIfNull(scored);
        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Id)
            .Select((x, index) => new Recommendation(index + 1, x.Item.Id, x.Item.Title, x.Score))
            .ToArray();
    }

    public static Recommendation[] Run(IEnumerable<Ticket> tickets, int maxEffort, Func<Ticket, int> policy)
    {
        var workable = SelectWorkable(tickets, maxEffort);
        var scored = ScoreTickets(workable, policy);
        return BuildRecommendations(scored);
    }
}
