using System.Collections.Generic;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: query execution failure (timeout, cancelled, not executed).
/// </summary>
public sealed class SurrealDbQueryException : SurrealDbRpcException
{
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
    /// The timeout duration, if this is a timeout error. Returns (seconds, nanos) or null if not a timeout error.
    /// </summary>
    public (int seconds, int nanos)? Timeout =>
        RpcErrorDetailHelpers.DetailTimeoutDuration(Details);

    internal SurrealDbQueryException(
        string message,
        string? kind,
        IReadOnlyDictionary<string, object?>? details,
        Exception? innerException = null
    )
        : base(message, details, innerException)
    {
        Kind = kind;
    }
}
