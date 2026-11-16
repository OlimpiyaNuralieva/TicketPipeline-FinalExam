public class ReportTests
{
    private static Ticket T(int id, int effort, bool open, TicketKind kind)
        => Helpers.Make(id, "T" + id, effort, open, kind);

    [Fact]
    public void WorkloadByKind_SampleData_GroupsCountsAndTotalsEffort()
    {
        var rows = Reports.WorkloadByKind(Helpers.SampleTickets());

        Assert.Equal(new[]
        {
            new WorkloadRow(TicketKind.Incident, 3, 13),
            new WorkloadRow(TicketKind.Request, 2, 12),
            new WorkloadRow(TicketKind.Question, 1, 2)
        }, rows);
    }

    [Fact]
    public void WorkloadByKind_EmptyDataset_AndNoOpenTickets_GiveEmptyReport()
    {
        Assert.Empty(Reports.WorkloadByKind(Array.Empty<Ticket>()));
        Assert.Empty(Reports.WorkloadByKind(new[] { T(1, 3, false, TicketKind.Incident) }));
    }

    [Fact]
    public void WorkloadByKind_TiedTotals_AreOrderedByKindWhateverTheInputOrder()
    {
        var a = T(1, 4, true, TicketKind.Request);
        var b = T(2, 4, true, TicketKind.Question);

        var forward = Reports.WorkloadByKind(new[] { a, b });
        var backward = Reports.WorkloadByKind(new[] { b, a });

        Assert.Equal(new[] { TicketKind.Question, TicketKind.Request }, forward.Select(r => r.Kind).ToArray());
        Assert.Equal(forward, backward);
    }

    [Fact]
    public void PriorityList_SampleData_RanksByConfiguredScore()
    {
        var rows = Reports.PriorityList(Helpers.SampleTickets(), 7, Scoring.Severity, 5);

        Assert.Equal(new[] { 3, 8, 1, 5, 6 }, rows.Select(r => r.Ticket.Id).ToArray());
        Assert.Equal(new[] { 35, 35, 31, 24, 16 }, rows.Select(r => r.Score).ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, rows.Select(r => r.Rank).ToArray());
    }

    [Fact]
    public void PriorityList_TiedScores_AreOrderedByIdWhateverTheInputOrder()
    {
        var low = T(3, 3, true, TicketKind.Incident);
        var high = T(8, 3, true, TicketKind.Incident);

        Assert.Equal(new[] { 3, 8 }, Reports.PriorityList(new[] { high, low }, 7, Scoring.Severity, 5).Select(r => r.Ticket.Id).ToArray());
        Assert.Equal(new[] { 3, 8 }, Reports.PriorityList(new[] { low, high }, 7, Scoring.Severity, 5).Select(r => r.Ticket.Id).ToArray());
    }

    [Fact]
    public void PriorityList_EmptyDatasetOrNoMatches_GiveEmptyReport()
    {
        Assert.Empty(Reports.PriorityList(Array.Empty<Ticket>(), 7, Scoring.Severity, 5));

        var noMatch = new[] { T(1, 3, false, TicketKind.Incident), T(2, 8, true, TicketKind.Request) };
        Assert.Empty(Reports.PriorityList(noMatch, 7, Scoring.Severity, 5));
    }
}
