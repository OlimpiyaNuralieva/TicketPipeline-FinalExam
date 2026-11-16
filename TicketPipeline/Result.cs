public sealed record Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("error code must not be empty", nameof(code));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("error message must not be empty", nameof(message));
        Code = code;
        Message = message;
    }
}

public static class ErrorCode
{
    public const string TicketInvalid = "ticket.invalid";
    public const string TicketFormat = "ticket.format";
    public const string TicketNotFound = "ticket.not_found";
    public const string NotAllowed = "workflow.not_allowed";
    public const string InvalidInput = "workflow.invalid_input";
    public const string FileMissing = "file.missing";
    public const string FileUnreadable = "file.unreadable";
    public const string DataMalformed = "data.malformed";
    public const string DuplicateId = "data.duplicate_id";
    public const string FileWriteFailed = "file.write_failed";
    public const string NativeInvalidInput = "native.invalid_input";
    public const string NativeUnavailable = "native.unavailable";
    public const string NativeRejected = "native.rejected";
    public const string AgentInvalid = "agent.invalid";
    public const string AgentFormat = "agent.format";
    public const string AgentNotFound = "agent.not_found";
    public const string DuplicateAgent = "data.duplicate_agent";
    public const string AlreadyAssigned = "assign.already_assigned";
    public const string OverCapacity = "assign.over_capacity";
}

public abstract record Result<T> where T : notnull
{
    private protected Result() { }

    public sealed record Ok : Result<T>
    {
        public T Value { get; }

        public Ok(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed record Fail : Result<T>
    {
        public Error Error { get; }

        public Fail(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }
    }
}

public abstract record Option<T> where T : notnull
{
    private protected Option() { }

    public sealed record Some : Option<T>
    {
        public T Value { get; }

        public Some(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed record None : Option<T>;
}

public static class Results
{
    public static Result<U> Map<T, U>(Result<T> result, Func<T, U> f)
        where T : notnull
        where U : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(f);
        return result switch
        {
            Result<T>.Ok ok => new Result<U>.Ok(f(ok.Value)),
            Result<T>.Fail fail => new Result<U>.Fail(fail.Error),
            _ => throw new ArgumentException("Unknown case", nameof(result))
        };
    }

    public static Result<U> Bind<T, U>(Result<T> result, Func<T, Result<U>> f)
        where T : notnull
        where U : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(f);
        return result switch
        {
            Result<T>.Ok ok => f(ok.Value),
            Result<T>.Fail fail => new Result<U>.Fail(fail.Error),
            _ => throw new ArgumentException("Unknown case", nameof(result))
        };
    }

    public static Result<T> ToResult<T>(Option<T> option, Error missing)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(missing);
        return option switch
        {
            Option<T>.Some some => new Result<T>.Ok(some.Value),
            Option<T>.None => new Result<T>.Fail(missing),
            _ => throw new ArgumentException("Unknown case", nameof(option))
        };
    }

    public static Result<T[]> Sequence<T>(IEnumerable<Result<T>> results)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(results);
        var values = new List<T>();
        foreach (Result<T> result in results)
        {
            switch (result)
            {
                case Result<T>.Ok ok:
                    values.Add(ok.Value);
                    break;
                case Result<T>.Fail fail:
                    return new Result<T[]>.Fail(fail.Error);
                default:
                    throw new ArgumentException("Unknown case", nameof(results));
            }
        }
        return new Result<T[]>.Ok(values.ToArray());
    }
}
