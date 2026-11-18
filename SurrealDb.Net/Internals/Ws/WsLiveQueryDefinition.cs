namespace SurrealDb.Net.Internals.Ws;

/// <summary>
/// What is needed to start a live query again on a new connection.
/// A live query is known by the id the server gave it first; <see cref="ServerId"/> changes each time it is resubscribed.
/// </summary>
internal sealed class WsLiveQueryDefinition
{
    public Guid ServerId { get; set; }
    public Guid? SessionId { get; init; }

    /// <summary>
    /// The namespace and database the live query was started in, which its session may have left since.
    /// </summary>
    public string? Ns { get; init; }
    public string? Db { get; init; }

    /// <summary>
    /// Who the session was signed in as when the live query was started, see <see cref="Auth.AuthIdentity"/>.
    /// </summary>
    public string? Identity { get; init; }

    public string? Table { get; init; }
    public bool Diff { get; init; }

    public string? Query { get; init; }
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
}
