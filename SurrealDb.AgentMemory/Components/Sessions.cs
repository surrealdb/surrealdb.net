using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Creates conversation sessions for a context.
/// </summary>
public sealed class Sessions
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _contextId;

    internal Sessions(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _contextId = contextId;
    }

    /// <summary>
    /// Opens a new session with an optional DNF scope selector and metadata.
    /// </summary>
    /// <param name="options">Scopes and metadata.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Session> CreateAsync(
        SessionCreateOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonBody().Add("scopes", options?.Scopes).Add("metadata", options?.Metadata);
        var response = await _transport
            .RequestRequiredAsync(
                HttpMethod.Post,
                $"{AgentMemoryPaths.GetContextApiPrefix(_contextId)}/sessions",
                AgentMemoryJsonContext.Default.SessionResponseJson,
                Json(body),
                cancellationToken
            )
            .ConfigureAwait(false);
        return new Session(_transport, _contextId, response);
    }
}

/// <summary>
/// An open conversation session within an Agent Memory context.
/// </summary>
public sealed class Session
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Session(AgentMemoryTransport transport, string contextId, SessionResponseJson info)
    {
        _transport = transport;
        Id = info.Id;
        CreatedAt = info.CreatedAt;
        Scopes = info.Scopes;
        _base =
            $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/sessions/{AgentMemoryPaths.EncodePathSegment(Id)}";
    }

    /// <summary>
    /// Session id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public string CreatedAt { get; }

    /// <summary>
    /// DNF scope selector the session writes to (outer OR, inner AND).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Scopes { get; }

    /// <summary>
    /// Deletes this session on the server.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _transport.SendAsync(HttpMethod.Delete, _base, null, cancellationToken);
    }

    /// <summary>
    /// Lists one page of the turns recorded against this session, oldest first.
    /// </summary>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<TurnListResponseJson> TurnsAsync(
        PageOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchTurnsAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("turns");
    }

    /// <summary>
    /// Every turn in this session, following cursors to exhaustion. A transcript is read whole,
    /// so this is usually what a caller wants.
    /// </summary>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<TurnResponseJson>> AllTurnsAsync(
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchTurnsAsync(new PageOptions { Limit = limit, Cursor = cursor }, token),
            page => page.Page,
            page => page.Turns,
            "turns",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Retrieves session-scoped LLM context text for a query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<SessionContextResponseJson> ContextAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        // A read behind a POST: safe to replay.
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/context",
            AgentMemoryJsonContext.Default.SessionContextResponseJson,
            Json(new JsonBody().Add("query", query), idempotent: true),
            cancellationToken
        );
    }

    private Task<TurnListResponseJson?> FetchTurnsAsync(
        PageOptions? options,
        CancellationToken cancellationToken
    )
    {
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{_base}/turns",
            AgentMemoryJsonContext.Default.TurnListResponseJson,
            Query(new QueryParameters().AddPage(options)),
            cancellationToken
        );
    }
}
