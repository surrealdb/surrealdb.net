using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Operator lifecycle sweeps: expiry and decay.
/// </summary>
public sealed class Lifecycle
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Lifecycle(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/lifecycle";
    }

    /// <summary>
    /// Runs the context-category expiry sweep. Returns the number of affected rows.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<LifecycleResponseJson> ExpireAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/expire",
            AgentMemoryJsonContext.Default.LifecycleResponseJson,
            Json(new JsonBody()),
            cancellationToken
        );
    }

    /// <summary>
    /// Runs the importance decay sweep. Returns the number of affected rows.
    /// </summary>
    /// <remarks>
    /// The sweep covers the whole context, so it requires <c>grant:manage</c> over the whole context.
    /// </remarks>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<LifecycleResponseJson> DecayAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/decay",
            AgentMemoryJsonContext.Default.LifecycleResponseJson,
            Json(new JsonBody()),
            cancellationToken
        );
    }
}

/// <summary>
/// Retrieval decision traces for a context.
/// </summary>
public sealed class Traces
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Traces(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/traces";
    }

    /// <summary>
    /// Lists one page of trace records, newest first.
    /// </summary>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<TraceListResponseJson> ListAsync(
        PageOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("traces");
    }

    /// <summary>
    /// Every trace record, following cursors to exhaustion.
    /// </summary>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<TraceRecordJson>> ListAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(new PageOptions { Limit = limit, Cursor = cursor }, token),
            page => page.Page,
            page => page.Traces,
            "traces",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Fetches one trace by id.
    /// </summary>
    /// <param name="traceId">The trace id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<TraceRecordJson> GetAsync(
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(traceId);
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(traceId)}",
            AgentMemoryJsonContext.Default.TraceRecordJson,
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// Aggregate trace statistics over the recent window.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<TraceStatsResponseJson> StatsAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/stats",
            AgentMemoryJsonContext.Default.TraceStatsResponseJson,
            null,
            cancellationToken
        );
    }

    private Task<TraceListResponseJson?> FetchListAsync(
        PageOptions? options,
        CancellationToken cancellationToken
    )
    {
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.TraceListResponseJson,
            Query(new QueryParameters().AddPage(options)),
            cancellationToken
        );
    }
}

/// <summary>
/// Principals and their scope grants. Reading requires a <c>grant:manage</c> grant; granting and
/// revoking require it over the pattern concerned.
/// </summary>
public sealed class Principals
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Principals(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/principals";
    }

    /// <summary>
    /// Lists one page of the context's principals.
    /// </summary>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PrincipalListResponseJson> ListAsync(
        PageOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("principals");
    }

    /// <summary>
    /// Every principal in the context, following cursors to exhaustion.
    /// </summary>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<PrincipalJson>> ListAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(new PageOptions { Limit = limit, Cursor = cursor }, token),
            page => page.Page,
            page => page.Principals,
            "principals",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Fetches one principal and its declared grants.
    /// </summary>
    /// <param name="principalId">The principal id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PrincipalJson> GetAsync(
        string principalId,
        CancellationToken cancellationToken = default
    )
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            PrincipalPath(principalId),
            AgentMemoryJsonContext.Default.PrincipalJson,
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// Resolves the verbs a principal effectively holds at a scope path.
    /// </summary>
    /// <param name="principalId">The principal id.</param>
    /// <param name="path">The scope path.</param>
    /// <param name="asOf">Resolves the grants as they stood at this RFC 3339 instant.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EffectiveGrantsJson> EffectiveAsync(
        string principalId,
        string path,
        string? asOf = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(path);

        var query = new QueryParameters().Add("path", path).Add("asOf", asOf);
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{PrincipalPath(principalId)}/effective",
            AgentMemoryJsonContext.Default.EffectiveGrantsJson,
            Query(query),
            cancellationToken
        );
    }

    /// <summary>
    /// Grants a principal a set of verbs over a scope pattern.
    /// </summary>
    /// <param name="principalId">The principal id.</param>
    /// <param name="path">The scope pattern.</param>
    /// <param name="verbs">The verbs (see <see cref="Verbs"/>).</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PrincipalJson> GrantAsync(
        string principalId,
        string path,
        IEnumerable<string> verbs,
        CancellationToken cancellationToken = default
    )
    {
        return ChangeGrantsAsync(HttpMethod.Post, principalId, path, verbs, cancellationToken);
    }

    /// <summary>
    /// Revokes a set of verbs from a principal over a scope pattern.
    /// </summary>
    /// <param name="principalId">The principal id.</param>
    /// <param name="path">The scope pattern.</param>
    /// <param name="verbs">The verbs (see <see cref="Verbs"/>).</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PrincipalJson> RevokeAsync(
        string principalId,
        string path,
        IEnumerable<string> verbs,
        CancellationToken cancellationToken = default
    )
    {
        return ChangeGrantsAsync(HttpMethod.Delete, principalId, path, verbs, cancellationToken);
    }

    private Task<PrincipalJson> ChangeGrantsAsync(
        HttpMethod method,
        string principalId,
        string path,
        IEnumerable<string> verbs,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(verbs);

        var body = new JsonBody().Add("path", path).Add("verbs", verbs);
        return _transport.RequestRequiredAsync(
            method,
            $"{PrincipalPath(principalId)}/grants",
            AgentMemoryJsonContext.Default.PrincipalJson,
            Json(body),
            cancellationToken
        );
    }

    private string PrincipalPath(string principalId)
    {
        ArgumentNullException.ThrowIfNull(principalId);
        return $"{_base}/{AgentMemoryPaths.EncodePathSegment(principalId)}";
    }

    private Task<PrincipalListResponseJson?> FetchListAsync(
        PageOptions? options,
        CancellationToken cancellationToken
    )
    {
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.PrincipalListResponseJson,
            Query(new QueryParameters().AddPage(options)),
            cancellationToken
        );
    }
}

/// <summary>
/// The scope tree: register, list, delete, and forget scope subtrees.
/// </summary>
public sealed class Scopes
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Scopes(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/scopes";
    }

    /// <summary>
    /// Lists one page of registered scope nodes.
    /// </summary>
    /// <remarks>
    /// A page can hold fewer than <c>limit</c> nodes while more remain: the server bounds the page and then drops
    /// the nodes the caller has no grant over. End a walk on <c>page.nextCursor</c>, never on a short page, or call
    /// <see cref="ListAllAsync"/>. The endpoint takes no <c>count</c>, so <c>page.totalSize</c> is always absent.
    /// </remarks>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<ScopeListResponseJson> ListAsync(
        CursorOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("scopes");
    }

    /// <summary>
    /// Every registered scope node, following cursors to exhaustion.
    /// The scope tree is consumed whole (as a tree to render, or as the source for scope autocomplete).
    /// </summary>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<ScopeNodeJson>> ListAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(new CursorOptions { Limit = limit, Cursor = cursor }, token),
            page => page.Page,
            page => page.Scopes,
            "scopes",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Registers a scope path with optional display metadata.
    /// </summary>
    /// <param name="path">The scope path.</param>
    /// <param name="displayName">A display name.</param>
    /// <param name="description">A description.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ScopeNodeJson> RegisterAsync(
        string path,
        string? displayName = null,
        string? description = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(path);

        var body = new JsonBody()
            .Add("path", path)
            .Add("displayName", displayName)
            .Add("description", description);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            _base,
            AgentMemoryJsonContext.Default.ScopeNodeJson,
            Json(body),
            cancellationToken
        );
    }

    /// <summary>
    /// Deletes (tombstones) a scope node by path.
    /// </summary>
    /// <param name="path">The scope path.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _transport.SendAsync(
            HttpMethod.Delete,
            _base,
            Query(new QueryParameters().Add("path", path)),
            cancellationToken
        );
    }

    /// <summary>
    /// Forgets (erases) everything stored under a scope subtree. Returns the number of rows forgotten.
    /// </summary>
    /// <remarks>
    /// Requires <c>memory:forget</c> over the subtree.
    /// </remarks>
    /// <param name="path">The subtree to forget.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ForgetScopeResponseJson> ForgetAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/forget",
            AgentMemoryJsonContext.Default.ForgetScopeResponseJson,
            Json(new JsonBody().Add("path", path)),
            cancellationToken
        );
    }

    private Task<ScopeListResponseJson?> FetchListAsync(
        CursorOptions? options,
        CancellationToken cancellationToken
    )
    {
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.ScopeListResponseJson,
            Query(new QueryParameters().AddCursor(options)),
            cancellationToken
        );
    }
}

/// <summary>
/// Self-service API keys for this context: the calling principal's own keys, or every key for a
/// caller that holds <c>grant:manage</c> over the whole context. The context has to allow self-service
/// keys, and a delegated client (<see cref="AgentMemoryClient.OnBehalfOf"/>) is refused.
/// </summary>
public sealed class Keys
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Keys(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/keys";
    }

    /// <summary>
    /// Mints a new key. The full secret is returned once, in <see cref="MintedKeyJson.Key"/>, and cannot be retrieved again.
    /// </summary>
    /// <param name="options">Name, grants and lifetime.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<MintedKeyJson> CreateAsync(
        KeyCreateOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonBody().Add("name", options?.Name).Add("grants", options?.Grants);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            _base,
            AgentMemoryJsonContext.Default.MintedKeyJson,
            new AgentMemoryRequestOptions
            {
                // No name and no grants means no body at all.
                Body = body.IsEmpty ? null : body.Build(),
                Query = new QueryParameters().Add("ttlSeconds", options?.TtlSeconds),
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Lists one page of key metadata. Secrets are never included.
    /// </summary>
    /// <remarks>
    /// A page can hold fewer than <c>limit</c> keys while more remain: a non-administrator sees only the key they
    /// authenticated with, so the page is bounded and then filtered. End a walk on <c>page.nextCursor</c>.
    /// </remarks>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<KeyListResponseJson> ListAsync(
        PageOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("keys");
    }

    /// <summary>
    /// Every key the caller can see, following cursors to exhaustion.
    /// </summary>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<KeyDetailJson>> ListAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(new PageOptions { Limit = limit, Cursor = cursor }, token),
            page => page.Page,
            page => page.Keys,
            "keys",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Revokes a key by name.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task DeleteAsync(string keyName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyName);
        return _transport.SendAsync(
            HttpMethod.Delete,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(keyName)}",
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// Rotates a key, returning a fresh secret in <see cref="MintedKeyJson.Key"/>.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <param name="ttlSeconds">Lifetime of the new secret, in seconds.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<MintedKeyJson> RotateAsync(
        string keyName,
        int? ttlSeconds = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(keyName);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(keyName)}/rotate",
            AgentMemoryJsonContext.Default.MintedKeyJson,
            Query(new QueryParameters().Add("ttlSeconds", ttlSeconds)),
            cancellationToken
        );
    }

    private Task<KeyListResponseJson?> FetchListAsync(
        PageOptions? options,
        CancellationToken cancellationToken
    )
    {
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.KeyListResponseJson,
            Query(new QueryParameters().AddPage(options)),
            cancellationToken
        );
    }
}
