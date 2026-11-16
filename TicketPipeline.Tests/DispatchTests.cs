public class DispatchTests
{
    private static readonly Agent[] Agents =
    {
        Helpers.ValueOf(Agent.Create("Anna", 10)),
        Helpers.ValueOf(Agent.Create("Chen", 4))
    };

    private static Result<AssignmentBook> Assign(AssignmentBook book, int id, string? who)
        => Dispatch.Assign(Helpers.SampleTickets(), Agents, book, id, who);

    [Fact]
    public void Assign_Success_ReturnsNewBook_AndKeepsTheOldBookUnchanged()
    {
        AssignmentBook before = AssignmentBook.Empty;

        AssignmentBook after = Helpers.ValueOf(Assign(before, 5, "chen"));

        Assert.Empty(before.Items);
        Assert.Equal(new[] { new Assignment(5, "Chen", 4) }, after.Items.ToArray());
    }

    [Fact]
    public void Assign_MissingTicket_FailsWithTicketNotFound()
        => Assert.Equal(ErrorCode.TicketNotFound, Helpers.ErrorOf(Assign(AssignmentBook.Empty, 99, "Anna")).Code);

    [Fact]
    public void Assign_MissingAgent_FailsWithAgentNotFound()
        => Assert.Equal(ErrorCode.AgentNotFound, Helpers.ErrorOf(Assign(AssignmentBook.Empty, 3, "Nobody")).Code);

    [Fact]
    public void Assign_ClosedTicket_IsNotAllowed()
        => Assert.Equal(ErrorCode.NotAllowed, Helpers.ErrorOf(Assign(AssignmentBook.Empty, 7, "Anna")).Code);

    [Fact]
    public void Assign_SameTicketTwice_FailsWithAlreadyAssigned_AndFirstAgentStays()
    {
        AssignmentBook first = Helpers.ValueOf(Assign(AssignmentBook.Empty, 3, "Anna"));

        Error error = Helpers.ErrorOf(Assign(first, 3, "Chen"));

        Assert.Equal(ErrorCode.AlreadyAssigned, error.Code);
        Assert.Equal("Anna", first.Items.Single().AgentName);
    }

    [Fact]
    public void Assign_OverCapacity_Fails_AndExactCapacityIsAccepted()
    {
        AssignmentBook full = Helpers.ValueOf(Assign(AssignmentBook.Empty, 5, "Chen"));

        Error error = Helpers.ErrorOf(Assign(full, 8, "Chen"));

        Assert.Equal(ErrorCode.OverCapacity, error.Code);
        Assert.Single(full.Items);
    }

    [Fact]
    public void Assign_FailureStopsTheChain_SoAnUnknownTicketNeverLooksAtAgents()
    {
        IEnumerable<Agent> poisoned = Poison();

        Result<AssignmentBook> result = Dispatch.Assign(Helpers.SampleTickets(), poisoned, AssignmentBook.Empty, 99, "Anna");

        Assert.Equal(ErrorCode.TicketNotFound, Helpers.ErrorOf(result).Code);

        static IEnumerable<Agent> Poison()
        {
            throw new InvalidOperationException("agents must not be read");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }

    [Fact]
    public void Save_WritesTheBook_AndAnUnwritablePathFailsWithFileWriteFailed()
    {
        AssignmentBook book = Helpers.ValueOf(Assign(AssignmentBook.Empty, 3, "Anna"));
        string path = TempFiles.NewPath();
        try
        {
            Helpers.ValueOf(Dispatch.Save(path, book));

            Assert.Equal(new[] { "ticket_id;agent;effort", "3;Anna;3" }, File.ReadAllLines(path));

            string badPath = Path.Combine(Path.GetTempPath(), "no-such-folder-" + Guid.NewGuid().ToString("N"), "a.csv");
            Assert.Equal(ErrorCode.FileWriteFailed, Helpers.ErrorOf(Dispatch.Save(badPath, book)).Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FullWorkflow_FromFiles_AssignsReportsAndSaves()
    {
        string tickets = TempFiles.NewPath();
        string agents = TempFiles.NewPath();
        string saved = TempFiles.NewPath();
        try
        {
            File.WriteAllText(tickets, TempFiles.SampleCsv);
            File.WriteAllText(agents, "Anna;10\nChen;4\n");

            Ticket[] loadedTickets = Helpers.ValueOf(TicketFile.Load(tickets));
            Agent[] loadedAgents = Helpers.ValueOf(AgentFile.Load(agents));

            AssignmentBook book = Helpers.ValueOf(Dispatch.Assign(loadedTickets, loadedAgents, AssignmentBook.Empty, 1, "Anna"));
            book = Helpers.ValueOf(Dispatch.Assign(loadedTickets, loadedAgents, book, 5, "Chen"));

            AgentLoadRow[] rows = Reports.AgentLoad(loadedAgents, book);
            Assert.Equal(new[] { "Anna", "Chen" }, rows.Select(r => r.Name).ToArray());
            Assert.Equal(new[] { 3, 0 }, rows.Select(r => r.Free).ToArray());

            Helpers.ValueOf(Dispatch.Save(saved, book));
            Assert.Equal(3, File.ReadAllLines(saved).Length);
        }
        finally
        {
            File.Delete(tickets);
            File.Delete(agents);
            File.Delete(saved);
        }
    }
}
