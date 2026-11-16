public static class TicketQueryExtensions
{
    public static IEnumerable<Ticket> Workable(this IEnumerable<Ticket> tickets, int maxEffort)
    {
        return tickets.Where(t => t.IsOpen && t.Effort <= maxEffort);
    }
}

public static class ResultExtensions
{
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
        where T : notnull
    {
        switch (result)
        {
            case Result<T>.Ok ok:
                if (predicate(ok.Value))
                    return ok;
                return new Result<T>.Fail(error);
            case Result<T>.Fail:
                return result;
            default:
                throw new ArgumentException("Unknown case", nameof(result));
        }
    }
}
