public static class Workflow
{
    public static Result<TicketState> Begin(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        if (!ticket.IsOpen)
            return Fail(ErrorCode.NotAllowed, $"ticket {ticket.Id} is closed and cannot enter the workflow");
        return new Result<TicketState>.Ok(new TicketState.New(ticket));
    }

    public static Result<TicketState> Assign(TicketState state, string? assignee)
    {
        ArgumentNullException.ThrowIfNull(state);
        string name = assignee?.Trim() ?? string.Empty;
        return state switch
        {
            TicketState.New when name.Length == 0
                => Fail(ErrorCode.InvalidInput, "assignee must not be empty"),
            TicketState.New n
                => new Result<TicketState>.Ok(new TicketState.Assigned(n.Ticket, name)),

            TicketState.Assigned a
                => Fail(ErrorCode.NotAllowed, $"ticket {a.Ticket.Id} is already assigned to {a.Assignee}"),
            TicketState.Resolved r
                => Fail(ErrorCode.NotAllowed, $"ticket {r.Ticket.Id} is already resolved"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }

    public static Result<TicketState> Resolve(TicketState state, string? resolution)
    {
        ArgumentNullException.ThrowIfNull(state);
        string text = resolution?.Trim() ?? string.Empty;
        return state switch
        {
            TicketState.New n
                => Fail(ErrorCode.NotAllowed, $"ticket {n.Ticket.Id} must be assigned before it can be resolved"),
            TicketState.Assigned when text.Length == 0
                => Fail(ErrorCode.InvalidInput, "resolution must not be empty"),
            TicketState.Assigned a
                => new Result<TicketState>.Ok(new TicketState.Resolved(a.Ticket, a.Assignee, text)),

            TicketState.Resolved r
                => Fail(ErrorCode.NotAllowed, $"ticket {r.Ticket.Id} is already resolved"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }

    public static string Describe(TicketState state) => state switch
    {
        TicketState.New n => $"[{n.Ticket.Id}] {n.Ticket.Title}: New",
        TicketState.Assigned a => $"[{a.Ticket.Id}] {a.Ticket.Title}: Assigned to {a.Assignee}",
        TicketState.Resolved r => $"[{r.Ticket.Id}] {r.Ticket.Title}: Resolved by {r.Assignee} ({r.Resolution})",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    public static Result<string> Process(IEnumerable<Ticket> tickets, int id, string? assignee, string? resolution)
    {
        Result<Ticket> found = Results.ToResult(
            Pipeline.FindTicket(tickets, id),
            new Error(ErrorCode.TicketNotFound, $"ticket {id} was not found"));

        Result<TicketState> started = Results.Bind(found, t => Begin(t));
        Result<TicketState> assigned = Results.Bind(started, s => Assign(s, assignee));
        Result<TicketState> resolved = Results.Bind(assigned, s => Resolve(s, resolution));
        return Results.Map(resolved, s => Describe(s));
    }

    private static Result<TicketState> Fail(string code, string message)
        => new Result<TicketState>.Fail(new Error(code, message));
}
