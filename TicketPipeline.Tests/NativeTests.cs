using System.Runtime.InteropServices;

internal static class BrokenBindings
{
    [DllImport("ticketcost", EntryPoint = "no_such_function")]
    internal static extern int MissingExport(int kind, int effort);
}

public class NativeTests
{
    [Theory]
    [InlineData(TicketKind.Question, 3, 6150)]
    [InlineData(TicketKind.Incident, 7, 40066)]
    public void RealNativeCall_ReturnsTheManuallyCalculatedCost(TicketKind kind, int effort, int expectedCents)
    {
        Assert.Equal(expectedCents, Helpers.ValueOf(NativeCost.Calculate(kind, effort, NativeCost.Real)));
    }

    [Theory]
    [InlineData(TicketKind.Incident, 0)]
    [InlineData((TicketKind)7, 3)]
    public void Adapter_InvalidInput_IsRejectedBeforeTheNativeCall(TicketKind kind, int effort)
    {
        int calls = 0;
        var error = Helpers.ErrorOf(NativeCost.Calculate(kind, effort, (k, e) => { calls++; return 1; }));

        Assert.Equal(ErrorCode.NativeInvalidInput, error.Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Adapter_InjectedLoadFailure_BecomesNativeUnavailable()
    {
        Func<int, int, int> failing = (k, e) => throw new DllNotFoundException("simulated");
        var error = Helpers.ErrorOf(NativeCost.Calculate(TicketKind.Incident, 3, failing));

        Assert.Equal(ErrorCode.NativeUnavailable, error.Code);
        Assert.Contains("DllNotFoundException", error.Message);
    }

    [Fact]
    public void Adapter_RealMissingExport_BecomesNativeUnavailable()
    {
        var error = Helpers.ErrorOf(NativeCost.Calculate(TicketKind.Incident, 3, BrokenBindings.MissingExport));
        Assert.Equal(ErrorCode.NativeUnavailable, error.Code);
    }
}
