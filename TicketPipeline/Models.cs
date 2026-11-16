using System.Collections.Immutable;

public enum TicketKind { Question, Request, Incident }

public sealed record Ticket
{
    public const int MinEffort = 1;
    public const int MaxEffort = 8;

    public int Id { get; }
    public string Title { get; }
    public int Effort { get; }
    public bool IsOpen { get; }
    public TicketKind Kind { get; }

    private Ticket(int id, string title, int effort, bool isOpen, TicketKind kind)
    {
        Id = id;
        Title = title;
        Effort = effort;
        IsOpen = isOpen;
        Kind = kind;
    }

    public static Result<Ticket> Create(int id, string? title, int effort, bool isOpen, TicketKind kind)
    {
        var reasons = new List<string>();
        string cleanTitle = title?.Trim() ?? string.Empty;

        if (id <= 0)
            reasons.Add("id must be a positive integer");
        if (cleanTitle.Length == 0)
            reasons.Add("title must not be empty");
        if (effort < MinEffort || effort > MaxEffort)
            reasons.Add("effort must be between 1 and 8, inclusive");
        if (!Enum.IsDefined(kind))
            reasons.Add("kind must be Question, Request or Incident");

        if (reasons.Count > 0)
            return new Result<Ticket>.Fail(new Error(ErrorCode.TicketInvalid, string.Join("; ", reasons)));

        return new Result<Ticket>.Ok(new Ticket(id, cleanTitle, effort, isOpen, kind));
    }
}

public abstract record TicketState
{
    private protected TicketState() { }

    public sealed record New(Ticket Ticket) : TicketState;
    public sealed record Assigned(Ticket Ticket, string Assignee) : TicketState;
    public sealed record Resolved(Ticket Ticket, string Assignee, string Resolution) : TicketState;
}

public record ScoredTicket(Ticket Item, int Score);
public record Recommendation(int Rank, int Id, string Title, int Score);

public sealed record Agent
{
    public const int MinCapacity = 1;
    public const int MaxCapacity = 40;

    public string Name { get; }
    public int Capacity { get; }

    private Agent(string name, int capacity)
    {
        Name = name;
        Capacity = capacity;
    }

    public static Result<Agent> Create(string? name, int capacity)
    {
        var reasons = new List<string>();
        string cleanName = name?.Trim() ?? string.Empty;

        if (cleanName.Length == 0)
            reasons.Add("name must not be empty");
        if (capacity < MinCapacity || capacity > MaxCapacity)
            reasons.Add($"capacity must be between {MinCapacity} and {MaxCapacity}, inclusive");

        if (reasons.Count > 0)
            return new Result<Agent>.Fail(new Error(ErrorCode.AgentInvalid, string.Join("; ", reasons)));

        return new Result<Agent>.Ok(new Agent(cleanName, capacity));
    }
}

public sealed record Assignment(int TicketId, string AgentName, int Effort);

public sealed record AssignmentBook
{
    public ImmutableArray<Assignment> Items { get; }

    private AssignmentBook(ImmutableArray<Assignment> items) => Items = items;

    public static AssignmentBook Empty { get; } = new(ImmutableArray<Assignment>.Empty);

    public AssignmentBook Add(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        return new AssignmentBook(Items.Add(assignment));
    }

    public bool IsAssigned(int ticketId) => Items.Any(a => a.TicketId == ticketId);

    public int LoadOf(string agentName)
        => Items.Where(a => string.Equals(a.AgentName, agentName, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.Effort);
}
