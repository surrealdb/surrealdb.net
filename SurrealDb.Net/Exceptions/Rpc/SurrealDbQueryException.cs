using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: query execution failure (timeout, cancelled, not executed).
/// </summary>
public sealed class SurrealDbQueryException : SurrealDbRpcException
{
    private readonly QueryErrorDetail? _details;

    public string? Kind { get; }

    /// <summary>
    /// True if the query was not executed (e.g. due to a prior error in the batch).
    /// </summary>
    public bool IsNotExecuted => Kind == "NotExecuted";

    /// <summary>
    /// True if the query timed out.
    /// </summary>
    public bool IsTimedOut => Kind == "TimedOut";

    /// <summary>
    /// True if the query was cancelled.
    /// </summary>
    public bool IsCancelled => Kind == "Cancelled";

    /// <summary>
    /// The timeout duration, if this is a timeout error. Returns `{ secs, nanos }` or undefined.
    /// </summary>
    public (int seconds, int nanos)? Timeout
    {
        get
        {
            return Kind != "TimedOut" || _details is null
                ? null
                : (_details.Seconds ?? 0, _details.Nanos ?? 0);
        }
    }

    internal SurrealDbQueryException(string message, string? kind, QueryErrorDetail? details)
        : base(message)
    {
        Kind = kind;
        _details = details;
    }
}
