public static class Helpers
{
    public static Ticket Make(int id, string title, int effort, bool isOpen, TicketKind kind)
        => Ticket.Create(id, title, effort, isOpen, kind) is Result<Ticket>.Ok ok
            ? ok.Value
            : throw new InvalidOperationException("test data must be valid");

    public static TValue ValueOf<TValue>(Result<TValue> result) where TValue : notnull
        => Assert.IsType<Result<TValue>.Ok>(result).Value;

    public static Error ErrorOf<TValue>(Result<TValue> result) where TValue : notnull
        => Assert.IsType<Result<TValue>.Fail>(result).Error;

    public static Ticket[] SampleTickets() => new[]
    {
        Make(8, "WiFi", 3, true, TicketKind.Incident),
        Make(3, "Login", 3, true, TicketKind.Incident),
        Make(5, "Printer", 4, true, TicketKind.Request),
        Make(1, "Server", 7, true, TicketKind.Incident),
        Make(7, "Email", 2, false, TicketKind.Incident),
        Make(6, "Account", 2, true, TicketKind.Question),
        Make(9, "Migration", 8, true, TicketKind.Request)
    };
}

public class PipelineTests
{
    private static int[] Ids(Recommendation[] recs) => recs.Select(r => r.Id).ToArray();

    [Fact]
    public void Run_TwoPolicies_GiveDifferentOrderOnSameInput()
    {
        var input = Helpers.SampleTickets();
        int[] severity = Ids(Pipeline.Run(input, 7, Pipeline.SeverityFirst));
        int[] quickWins = Ids(Pipeline.Run(input, 7, Pipeline.QuickWinsFirst));

        Assert.Equal(new[] { 3, 8, 1, 5, 6 }, severity);
        Assert.Equal(new[] { 6, 3, 8, 5, 1 }, quickWins);
    }

    [Fact]
    public void ParseTickets_OneBadLine_FailsWithLineNumberAndReturnsNoTickets()
    {
        var result = Pipeline.ParseTickets(new[] { "1;A;2;true;Question", "bad", "3;C;2;true;Request" });
        var error = Helpers.ErrorOf(result);
        Assert.Equal(ErrorCode.DataMalformed, error.Code);
        Assert.StartsWith("line 2:", error.Message);
    }
}
