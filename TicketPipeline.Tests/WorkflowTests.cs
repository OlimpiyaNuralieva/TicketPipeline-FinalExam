public class WorkflowTests
{
    private static Ticket Open() => Helpers.Make(3, "Login", 3, true, TicketKind.Incident);

    private static TicketState NewState() => Helpers.ValueOf(Workflow.Begin(Open()));
    private static TicketState AssignedState() => Helpers.ValueOf(Workflow.Assign(NewState(), "Anna"));

    [Fact]
    public void Resolve_BeforeAssign_FailsWithNotAllowed()
    {
        var error = Helpers.ErrorOf(Workflow.Resolve(NewState(), "done"));
        Assert.Equal(ErrorCode.NotAllowed, error.Code);
        Assert.Contains("must be assigned", error.Message);
    }

    [Fact]
    public void FailedTransition_DoesNotChangeOriginalState()
    {
        var assigned = AssignedState();
        var snapshot = AssignedState();

        Workflow.Assign(assigned, "Boris");
        Workflow.Resolve(assigned, "  ");

        Assert.Equal(snapshot, assigned);
    }
}
