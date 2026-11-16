public class ExtensionTests
{
    [Fact]
    public void Workable_KeepsOpenTicketsWithinMaxEffort_AndDoesNotChangeTheSource()
    {
        var input = Helpers.SampleTickets();
        int[] before = input.Select(t => t.Id).ToArray();

        int[] ids = input.Workable(7).Select(t => t.Id).ToArray();

        Assert.Equal(new[] { 8, 3, 5, 1, 6 }, ids);
        Assert.Equal(before, input.Select(t => t.Id).ToArray());
        Assert.Empty(Array.Empty<Ticket>().Workable(7));
    }

    [Fact]
    public void Ensure_OkThatPasses_IsKept_OkThatFails_BecomesTheSuppliedError()
    {
        var error = new Error("test.rule", "rule broken");
        Result<int> five = new Result<int>.Ok(5);

        Assert.Equal(5, Helpers.ValueOf(five.Ensure(n => n > 0, error)));
        Assert.Same(error, Helpers.ErrorOf(five.Ensure(n => n > 10, error)));
    }

    [Fact]
    public void Ensure_OnFailure_KeepsTheErrorAndDoesNotCallThePredicate()
    {
        int calls = 0;
        var original = new Error("test.original", "first failure");
        Result<int> failed = new Result<int>.Fail(original);

        var result = failed.Ensure(n => { calls++; return true; }, new Error("test.other", "other"));

        Assert.Equal(0, calls);
        Assert.Same(original, Helpers.ErrorOf(result));
    }
}
