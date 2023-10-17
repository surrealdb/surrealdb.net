namespace SurrealDb.Net.Models.Response;

/// <summary>
/// The base SurrealDB result interface, result of a query request.
/// </summary>
public interface ISurrealDbResult
{
    public bool IsOk { get; }
    public bool IsError => !IsOk;
}

/// <summary>
/// The base SurrealDB error result interface, result of a query request.
/// </summary>
public interface ISurrealDbErrorResult : ISurrealDbResult { }
