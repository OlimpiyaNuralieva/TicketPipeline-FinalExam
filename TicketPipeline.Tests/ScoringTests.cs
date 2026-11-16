public class ScoringTests
{
    [Fact]
    public void TwoPartiallyAppliedFunctions_GiveDifferentResultsOnTheSameTicket()
    {
        Func<Ticket, int> severity = Scoring.CreateScore(Scoring.SeverityConfig);
        Func<Ticket, int> quickWins = Scoring.CreateScore(Scoring.QuickWinsConfig);

        var login = Helpers.Make(3, "Login", 3, true, TicketKind.Incident);
        var server = Helpers.Make(1, "Server", 7, true, TicketKind.Incident);

        Assert.Equal(35, severity(login));
        Assert.Equal(53, quickWins(login));
        Assert.Equal(31, severity(server));
        Assert.Equal(13, quickWins(server));
    }

    [Fact]
    public void CreateScore_IsCurried_BothCallsInOneExpressionGiveTheSameAsTheConfiguredFunction()
    {
        var account = Helpers.Make(6, "Account", 2, true, TicketKind.Question);
        Assert.Equal(61, Scoring.CreateScore(Scoring.QuickWinsConfig)(account));
        Assert.Equal(Scoring.QuickWins(account), Scoring.CreateScore(Scoring.QuickWinsConfig)(account));
    }
}
