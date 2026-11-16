public sealed record PricedRow(int Rank, int Id, string Title, int Score, int CostCents);
public sealed record QueueReport(WorkloadRow[] Workload, PricedRow[] Priority, long TotalCostCents);

public static class ReportWorkflow
{
    public static Result<QueueReport> Build(
        string ticketsPath, int maxEffort, Func<Ticket, int> score, int limit, Func<int, int, int> native)
    {
        Result<Ticket[]> loaded = TicketFile.Load(ticketsPath)
            .Ensure(ts => ts.Select(t => t.Id).Distinct().Count() == ts.Length,
                    new Error(ErrorCode.DuplicateId, $"{ticketsPath}: ticket ids must be unique"));

        return Results.Bind(loaded, tickets => BuildFrom(tickets, maxEffort, score, limit, native));
    }

    public static Result<QueueReport> BuildFrom(
        Ticket[] tickets, int maxEffort, Func<Ticket, int> score, int limit, Func<int, int, int> native)
    {
        ArgumentNullException.ThrowIfNull(tickets);

        WorkloadRow[] workload = Reports.WorkloadByKind(tickets);
        PriorityRow[] ranked = Reports.PriorityList(tickets, maxEffort, score, limit);

        Result<PricedRow[]> priced = Results.Sequence(ranked.Select(row => Price(row, native)));
        return Results.Map(priced, rows => new QueueReport(workload, rows, rows.Sum(r => (long)r.CostCents)));
    }

    public static Result<string> BuildAndExport(
        string ticketsPath, string exportPath, int maxEffort, Func<Ticket, int> score, int limit, Func<int, int, int> native)
    {
        Result<QueueReport> report = Build(ticketsPath, maxEffort, score, limit, native);
        return Results.Bind(report, r => ReportFile.Export(exportPath, ReportFormat.ToCsvLines(r)));
    }

    private static Result<PricedRow> Price(PriorityRow row, Func<int, int, int> native)
        => Results.Map(
            NativeCost.Calculate(row.Ticket, native),
            cents => new PricedRow(row.Rank, row.Ticket.Id, row.Ticket.Title, row.Score, cents));
}
