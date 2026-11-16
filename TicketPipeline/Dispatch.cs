public static class Dispatch
{
    public static Option<Agent> FindAgent(IEnumerable<Agent> agents, string? name)
    {
        ArgumentNullException.ThrowIfNull(agents);
        string wanted = name?.Trim() ?? string.Empty;
        foreach (Agent agent in agents)
        {
            if (string.Equals(agent.Name, wanted, StringComparison.OrdinalIgnoreCase))
                return new Option<Agent>.Some(agent);
        }
        return new Option<Agent>.None();
    }

    public static Result<AssignmentBook> Assign(
        IEnumerable<Ticket> tickets, IEnumerable<Agent> agents, AssignmentBook book, int ticketId, string? agentName)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(book);

        Result<Ticket> ticket = Results.ToResult(
            Pipeline.FindTicket(tickets, ticketId),
            new Error(ErrorCode.TicketNotFound, $"ticket {ticketId} was not found"));

        Result<Assignment> planned = Results.Bind(ticket, t =>
            Results.Bind(
                Results.ToResult(
                    FindAgent(agents, agentName),
                    new Error(ErrorCode.AgentNotFound, $"agent '{agentName}' was not found")),
                a => Plan(book, t, a)));

        return Results.Map(planned, assignment => book.Add(assignment));
    }

    private static Result<Assignment> Plan(AssignmentBook book, Ticket ticket, Agent agent)
    {
        int load = book.LoadOf(agent.Name);
        bool fits = load + ticket.Effort <= agent.Capacity;

        return (ticket.IsOpen, book.IsAssigned(ticket.Id), fits) switch
        {
            (false, _, _) => Fail(ErrorCode.NotAllowed,
                $"ticket {ticket.Id} is closed and cannot be assigned"),
            (_, true, _) => Fail(ErrorCode.AlreadyAssigned,
                $"ticket {ticket.Id} already has an agent"),
            (_, _, false) => Fail(ErrorCode.OverCapacity,
                $"{agent.Name} has {agent.Capacity - load} free effort, ticket {ticket.Id} needs {ticket.Effort}"),
            _ => new Result<Assignment>.Ok(new Assignment(ticket.Id, agent.Name, ticket.Effort))
        };
    }

    private static Result<Assignment> Fail(string code, string message)
        => new Result<Assignment>.Fail(new Error(code, message));

    public static string[] ToCsvLines(AssignmentBook book)
    {
        ArgumentNullException.ThrowIfNull(book);
        var lines = new List<string> { "ticket_id;agent;effort" };
        lines.AddRange(book.Items.Select(a => $"{a.TicketId};{a.AgentName};{a.Effort}"));
        return lines.ToArray();
    }

    public static Result<string> Save(string path, AssignmentBook book)
        => ReportFile.Export(path, ToCsvLines(book));
}
