public class ResultTests
{
    private static Error SomeError() => new Error("test.error", "something failed");

    [Fact]
    public void Map_Success_TransformationReceivesValueAndReturnsNewValue()
    {
        int received = 0;
        Result<int> input = new Result<int>.Ok(3);

        var mapped = Results.Map(input, n =>
        {
            received = n;
            return n * 2;
        });

        Assert.Equal(3, received);
        Assert.Equal(6, Helpers.ValueOf(mapped));
    }

    [Fact]
    public void Bind_Success_NextOperationReceivesValueAndItsResultIsKept()
    {
        int received = 0;
        Result<int> input = new Result<int>.Ok(4);

        var next = Results.Bind<int, string>(input, q =>
        {
            received = q;
            return new Result<string>.Ok("quantity " + q);
        });
        Assert.Equal(4, received);
        Assert.Equal("quantity 4", Helpers.ValueOf(next));

        Error rejection = SomeError();
        var rejected = Results.Bind<int, string>(input, q => new Result<string>.Fail(rejection));
        Assert.Same(rejection, Helpers.ErrorOf(rejected));
    }

    [Fact]
    public void Map_AndBind_OnFailure_KeepTheErrorAndDoNotCallTheFunction()
    {
        int calls = 0;
        Error original = SomeError();
        Result<int> failed = new Result<int>.Fail(original);

        var mapped = Results.Map(failed, n =>
        {
            calls++;
            return n + 1;
        });
        var bound = Results.Bind<int, int>(failed, n =>
        {
            calls++;
            return new Result<int>.Ok(n + 1);
        });

        Assert.Equal(0, calls);
        Assert.Same(original, Helpers.ErrorOf(mapped));
        Assert.Same(original, Helpers.ErrorOf(bound));
    }

    [Fact]
    public void Process_AllStepsSucceed_ReturnsTheDescription()
    {
        var result = Workflow.Process(Helpers.SampleTickets(), 3, "Anna", "restarted router");
        Assert.Equal("[3] Login: Resolved by Anna (restarted router)", Helpers.ValueOf(result));
    }
}
