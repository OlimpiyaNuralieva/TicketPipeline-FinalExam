public class TicketTests
{
    [Fact]
    public void Create_ValidTicket_TrimsTheTitle_AndKeepsTheValues()
    {
        Ticket ticket = Helpers.ValueOf(Ticket.Create(3, "  Login ", 3, true, TicketKind.Incident));

        Assert.Equal(3, ticket.Id);
        Assert.Equal("Login", ticket.Title);
        Assert.Equal(3, ticket.Effort);
        Assert.True(ticket.IsOpen);
        Assert.Equal(TicketKind.Incident, ticket.Kind);
    }

    [Theory]
    [InlineData(0, "A", 3)]
    [InlineData(-5, "A", 3)]
    [InlineData(1, "", 3)]
    [InlineData(1, "   ", 3)]
    [InlineData(1, "A", 0)]
    [InlineData(1, "A", 9)]
    public void Create_BrokenRule_FailsWithTicketInvalid(int id, string title, int effort)
    {
        Error error = Helpers.ErrorOf(Ticket.Create(id, title, effort, true, TicketKind.Request));

        Assert.Equal(ErrorCode.TicketInvalid, error.Code);
    }

    [Fact]
    public void Create_BoundaryEfforts_AreAccepted()
    {
        Assert.Equal(Ticket.MinEffort, Helpers.ValueOf(Ticket.Create(1, "A", Ticket.MinEffort, true, TicketKind.Question)).Effort);
        Assert.Equal(Ticket.MaxEffort, Helpers.ValueOf(Ticket.Create(1, "A", Ticket.MaxEffort, true, TicketKind.Question)).Effort);
    }

    [Fact]
    public void Create_UnknownKindAndNullTitle_AreRejected_AndEveryBrokenRuleIsReported()
    {
        Assert.Equal(ErrorCode.TicketInvalid, Helpers.ErrorOf(Ticket.Create(1, "A", 3, true, (TicketKind)7)).Code);
        Assert.Equal(ErrorCode.TicketInvalid, Helpers.ErrorOf(Ticket.Create(1, null, 3, true, TicketKind.Request)).Code);

        Error all = Helpers.ErrorOf(Ticket.Create(-5, "", 99, true, TicketKind.Incident));

        Assert.Contains("id", all.Message);
        Assert.Contains("title", all.Message);
        Assert.Contains("effort", all.Message);
    }

    [Fact]
    public void ParseTicket_ValidLine_BuildsTheTicket_IgnoringSpacesAndCaseOfTheKind()
    {
        Ticket ticket = Helpers.ValueOf(Pipeline.ParseTicket(" 3 ; Login ; 3 ; true ; incident "));

        Assert.Equal(3, ticket.Id);
        Assert.Equal("Login", ticket.Title);
        Assert.Equal(3, ticket.Effort);
        Assert.Equal(TicketKind.Incident, ticket.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1;A;2;true")]
    [InlineData("x;A;2;true;Question")]
    [InlineData("1;A;two;true;Question")]
    [InlineData("1;A;2;maybe;Question")]
    [InlineData("1;A;2;true;Bug")]
    [InlineData("1;A;2;true;3")]
    public void ParseTicket_BadFormat_FailsWithTicketFormat(string? line)
    {
        Error error = Helpers.ErrorOf(Pipeline.ParseTicket(line));

        Assert.Equal(ErrorCode.TicketFormat, error.Code);
    }

    [Fact]
    public void ParseTicket_WellFormedLineBreakingARule_FailsWithTicketInvalid_NotTicketFormat()
    {
        Error error = Helpers.ErrorOf(Pipeline.ParseTicket("1;A;9;true;Question"));

        Assert.Equal(ErrorCode.TicketInvalid, error.Code);
    }

    [Fact]
    public void FindTicket_ReturnsSomeForAPresentTicket_AndNoneForMissingOrEmptyData()
    {
        Ticket[] tickets = Helpers.SampleTickets();

        var found = Assert.IsType<Option<Ticket>.Some>(Pipeline.FindTicket(tickets, 7));
        Assert.False(found.Value.IsOpen);

        Assert.IsType<Option<Ticket>.None>(Pipeline.FindTicket(tickets, 99));
        Assert.IsType<Option<Ticket>.None>(Pipeline.FindTicket(Array.Empty<Ticket>(), 1));
    }

    [Fact]
    public void Process_EachFailure_StopsTheChainWithItsOwnCode()
    {
        Ticket[] tickets = Helpers.SampleTickets();

        Assert.Equal(ErrorCode.TicketNotFound, Helpers.ErrorOf(Workflow.Process(tickets, 99, "Anna", "done")).Code);
        Assert.Equal(ErrorCode.NotAllowed, Helpers.ErrorOf(Workflow.Process(tickets, 7, "Anna", "done")).Code);
        Assert.Equal(ErrorCode.InvalidInput, Helpers.ErrorOf(Workflow.Process(tickets, 3, "  ", "done")).Code);
        Assert.Equal(ErrorCode.InvalidInput, Helpers.ErrorOf(Workflow.Process(tickets, 3, "Anna", "")).Code);
    }
}
