using System.Runtime.InteropServices;

internal static class NativeMethods
{
    [DllImport("ticketcost", EntryPoint = "ticket_cost_cents", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int TicketCostCents(int kind, int effort);
}

public static class NativeCost
{
    public static readonly Func<int, int, int> Real = NativeMethods.TicketCostCents;

    public static Result<int> Calculate(Ticket ticket, Func<int, int, int> native)
        => Calculate(ticket.Kind, ticket.Effort, native);

    public static Result<int> Calculate(TicketKind kind, int effort, Func<int, int, int> native)
    {
        if (!Enum.IsDefined(kind))
            return Fail(ErrorCode.NativeInvalidInput, $"kind {(int)kind} is not a known ticket kind");
        if (effort < Ticket.MinEffort || effort > Ticket.MaxEffort)
            return Fail(ErrorCode.NativeInvalidInput, $"effort {effort} must be between {Ticket.MinEffort} and {Ticket.MaxEffort}, inclusive");

        int cents;
        try
        {
            cents = native((int)kind, effort);
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is BadImageFormatException)
        {
            return Fail(ErrorCode.NativeUnavailable,
                $"native library 'ticketcost' cannot be used ({ex.GetType().Name}): {ex.Message}");
        }

        if (cents < 0)
            return Fail(ErrorCode.NativeRejected,
                $"native function rejected kind {(int)kind}, effort {effort} (error code {cents})");

        return new Result<int>.Ok(cents);
    }

    private static Result<int> Fail(string code, string message)
        => new Result<int>.Fail(new Error(code, message));
}
