namespace SurrealDb.Net.Internals.Ws;

/// <summary>
/// What is needed to start a live query again on a new connection.
/// A live query is known by the id the server gave it first; <see cref="ServerId"/> changes each time it is resubscribed.
/// </summary>
internal sealed class WsLiveQueryDefinition
{
    public Guid ServerId { get; set; }
    public Guid? SessionId { get; init; }

    public string? Table { get; init; }
    public bool Diff { get; init; }

    public string? Query { get; init; }
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
}
