public class AgentTests
{
    private static Agent A(string name, int capacity)
        => Helpers.ValueOf(Agent.Create(name, capacity));

    [Fact]
    public void Create_ValidAgent_TrimsTheName()
    {
        Agent agent = A("  Anna ", 10);

        Assert.Equal("Anna", agent.Name);
        Assert.Equal(10, agent.Capacity);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("   ", 5)]
    [InlineData("Anna", 0)]
    [InlineData("Anna", 41)]
    public void Create_BrokenRule_FailsWithAgentInvalid(string name, int capacity)
    {
        Error error = Helpers.ErrorOf(Agent.Create(name, capacity));

        Assert.Equal(ErrorCode.AgentInvalid, error.Code);
    }

    [Fact]
    public void Create_BoundaryCapacities_AreAccepted()
    {
        Assert.Equal(Agent.MinCapacity, A("A", Agent.MinCapacity).Capacity);
        Assert.Equal(Agent.MaxCapacity, A("A", Agent.MaxCapacity).Capacity);
    }

    [Fact]
    public void ParseAgent_BadFormat_FailsWithAgentFormat()
    {
        Assert.Equal(ErrorCode.AgentFormat, Helpers.ErrorOf(AgentParsing.ParseAgent("Anna")).Code);
        Assert.Equal(ErrorCode.AgentFormat, Helpers.ErrorOf(AgentParsing.ParseAgent("Anna;ten")).Code);
        Assert.Equal(ErrorCode.AgentFormat, Helpers.ErrorOf(AgentParsing.ParseAgent(null)).Code);
    }

    [Fact]
    public void ParseAgents_ListsEveryBadLine_AndReturnsNoAgents()
    {
        Error error = Helpers.ErrorOf(AgentParsing.ParseAgents(new[] { "Anna;10", "Boris;zero", ";5", "Chen;99" }));

        Assert.Equal(ErrorCode.DataMalformed, error.Code);
        Assert.Contains("line 2", error.Message);
        Assert.Contains("line 3", error.Message);
        Assert.Contains("line 4", error.Message);
        Assert.DoesNotContain("line 1", error.Message);
    }

    [Fact]
    public void ParseAgents_DuplicateNames_IgnoringCase_Fail_AndEmptyInputIsValid()
    {
        Assert.Equal(ErrorCode.DuplicateAgent, Helpers.ErrorOf(AgentParsing.ParseAgents(new[] { "Anna;5", "ANNA;6" })).Code);
        Assert.Empty(Helpers.ValueOf(AgentParsing.ParseAgents(Array.Empty<string>())));
    }

    [Fact]
    public void Load_MissingFile_FailsWithFileMissing()
    {
        string path = TempFiles.NewPath();

        Error error = Helpers.ErrorOf(AgentFile.Load(path));

        Assert.Equal(ErrorCode.FileMissing, error.Code);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Load_MalformedContent_Fails_AndValidContentLoads()
    {
        string bad = TempFiles.NewPath();
        string good = TempFiles.NewPath();
        try
        {
            File.WriteAllText(bad, "Anna;10\nBoris;zero\n");
            File.WriteAllText(good, "Anna;10\nBoris;6\n");

            Assert.Equal(ErrorCode.DataMalformed, Helpers.ErrorOf(AgentFile.Load(bad)).Code);
            Assert.Equal(new[] { "Anna", "Boris" }, Helpers.ValueOf(AgentFile.Load(good)).Select(a => a.Name).ToArray());
        }
        finally
        {
            File.Delete(bad);
            File.Delete(good);
        }
    }

    [Fact]
    public void AgentLoad_NoAssignments_EveryAgentIsFree_OrderedByFreeThenName()
    {
        var agents = new[] { A("Boris", 6), A("Anna", 6), A("Chen", 10) };

        AgentLoadRow[] rows = Reports.AgentLoad(agents, AssignmentBook.Empty);

        Assert.Equal(new[] { "Chen", "Anna", "Boris" }, rows.Select(r => r.Name).ToArray());
        Assert.All(rows, r => Assert.Equal(0, r.Used));
    }

    [Fact]
    public void AgentLoad_WithAssignments_SumsEffortPerAgent_AndEmptyAgentListGivesEmptyReport()
    {
        var agents = new[] { A("Anna", 10), A("Boris", 6) };
        AssignmentBook book = AssignmentBook.Empty
            .Add(new Assignment(1, "anna", 7))
            .Add(new Assignment(2, "Anna", 2))
            .Add(new Assignment(3, "Boris", 1));

        AgentLoadRow[] rows = Reports.AgentLoad(agents, book);

        Assert.Equal(new AgentLoadRow("Boris", 6, 1, 5, 1), rows[0]);
        Assert.Equal(new AgentLoadRow("Anna", 10, 9, 1, 2), rows[1]);
        Assert.Empty(Reports.AgentLoad(Array.Empty<Agent>(), book));
    }
}
