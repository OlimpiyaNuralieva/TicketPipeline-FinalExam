public sealed record WorkloadRow(TicketKind Kind, int Count, int TotalEffort);
public sealed record PriorityRow(int Rank, Ticket Ticket, int Score);
public sealed record AgentLoadRow(string Name, int Capacity, int Used, int Free, int Tickets);

public static class Reports
{
    public static WorkloadRow[] WorkloadByKind(IEnumerable<Ticket> tickets)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        return tickets
            .Where(t => t.IsOpen)
            .GroupBy(t => t.Kind)
            .Select(g => new WorkloadRow(g.Key, g.Count(), g.Sum(t => t.Effort)))
            .OrderByDescending(r => r.TotalEffort)
            .ThenBy(r => r.Kind)
            .ToArray();
    }

    public static PriorityRow[] PriorityList(IEnumerable<Ticket> tickets, int maxEffort, Func<Ticket, int> score, int limit)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentNullException.ThrowIfNull(score);
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        return Streams.ScoreLazily(tickets.Workable(maxEffort), score)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Item.Id)
            .Take(limit)
            .Select((s, index) => new PriorityRow(index + 1, s.Item, s.Score))
            .ToArray();
    }

    public static AgentLoadRow[] AgentLoad(IEnumerable<Agent> agents, AssignmentBook book)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(book);

        var byAgent = book.Items
            .GroupBy(a => a.AgentName.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => (Effort: g.Sum(a => a.Effort), Count: g.Count()));

        return agents
            .Select(a =>
            {
                var (effort, count) = byAgent.TryGetValue(a.Name.ToUpperInvariant(), out var found) ? found : (0, 0);
                return new AgentLoadRow(a.Name, a.Capacity, effort, a.Capacity - effort, count);
            })
            .OrderByDescending(r => r.Free)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
