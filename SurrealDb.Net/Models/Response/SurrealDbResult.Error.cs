using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB error result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbErrorResult : ISurrealDbErrorResult
{
    /// <summary>
    /// The kind of error thrown.
    /// </summary>
    public RpcErrorKind Kind { get; private set; }

    /// <summary>
    /// Time taken to execute the query.
    /// </summary>
    public TimeSpan Time { get; private set; }

    /// <summary>
    /// Status of the query ("ERR").
    /// </summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>
    /// Details about the error.
    /// </summary>
    public string Details { get; private init; } = string.Empty;

    public bool IsOk => false;

    internal SurrealDbErrorResult(RpcErrorKind kind, TimeSpan time, string status, string details)
    {
        Kind = kind;
        Time = time;
        Status = status;
        Details = details;
    }
}
