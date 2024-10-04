namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB error result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbErrorResult : ISurrealDbErrorResult
{
    /// <summary>
    /// Time taken to execute the query.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Status of the query ("ERR").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Details about the error.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    public bool IsOk => false;

    internal SurrealDbErrorResult(TimeSpan time, string status, string details)
    {
        Time = time;
        Status = status;
        Details = details;
    }
}
