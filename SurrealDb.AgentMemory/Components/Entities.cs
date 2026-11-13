using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Entity records, attributes, relations, name search, and attribute history.
/// </summary>
public sealed class Entities
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Entities(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/entities";
    }

    /// <summary>
    /// Lists one page of entities.
    /// </summary>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<EntityListResponseJson> ListAsync(
        EntityListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("entities");
    }

    /// <summary>
    /// Every matching entity, following cursors to exhaustion.
    /// </summary>
    /// <param name="type">Only entities of this type.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<EntityDetailJson>> ListAllAsync(
        string? type = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(
                    new EntityListOptions
                    {
                        Type = type,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Entities,
            "entities",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// How many entities match, without fetching them.
    /// </summary>
    /// <param name="type">Only entities of this type.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<long> CountAsync(
        string? type = null,
        CancellationToken cancellationToken = default
    )
    {
        var page = await ListAsync(
                new EntityListOptions
                {
                    Type = type,
                    Limit = 1,
                    Count = true,
                },
                cancellationToken
            )
            .ConfigureAwait(false);
        return page.Page.TotalSize ?? page.Entities.Count;
    }

    /// <summary>
    /// Searches entities by name, best match first (<c>GET /entities/search</c>).
    /// </summary>
    /// <remarks>
    /// Lexical and deterministic: an identical query returns identical rows in an identical order.
    /// An exact match on the normalized name scores <c>1.0</c> and everything else strictly below it.
    /// Each match carries its own <c>factCount</c> and a <c>distinguisher</c>, so two same-named candidates can be
    /// told apart. This is a ranked head, not a walk: it is bounded by the limit and offers no cursor.
    /// </remarks>
    /// <param name="query">The name to search for.</param>
    /// <param name="options">Type filter and limit.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<List<EntityMatchJson>> SearchAsync(
        string query,
        EntitySearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new QueryParameters()
            .Add("q", query)
            .Add("type", options?.Type)
            .Add("limit", options?.Limit);
        var response = await _transport
            .RequestRequiredAsync(
                HttpMethod.Get,
                $"{_base}/search",
                AgentMemoryJsonContext.Default.EntitySearchResponseJson,
                Query(parameters),
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Matches;
    }

    /// <summary>
    /// The entities worth starting from (<c>GET /entities/top</c>).
    /// </summary>
    /// <remarks>
    /// A ranked head, like <see cref="SearchAsync"/>: bounded, with no cursor.
    /// See <see cref="EntityRankings"/> for the cost of each ordering.
    /// </remarks>
    /// <param name="options">Ordering, type filter and limit.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<List<EntityMatchJson>> TopAsync(
        TopEntitiesOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new QueryParameters()
            .Add("by", options?.By)
            .Add("type", options?.Type)
            .Add("limit", options?.Limit);
        var response = await _transport
            .RequestRequiredAsync(
                HttpMethod.Get,
                $"{_base}/top",
                AgentMemoryJsonContext.Default.TopEntitiesResponseJson,
                Query(parameters),
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Entities;
    }

    /// <summary>
    /// Fetches one entity with a bounded head of its attributes and relations, newest first.
    /// </summary>
    /// <remarks>
    /// Both fact sections are bounded and report whether they were cut in <c>truncated</c>. To read one in full,
    /// walk its own collection: the attributes through <see cref="Facts.AllAttributesAsync"/>, and the relations
    /// through <see cref="Facts.AllEdgesOfAsync"/>, because the head carries edges in either direction.
    /// </remarks>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="options">The bound and the temporal filters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EntityResponseJson> GetAsync(
        string entityType,
        string name,
        EntityGetOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = new QueryParameters().Add("limit", options?.Limit).AddTemporal(options);
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            EntityPath(entityType, name),
            AgentMemoryJsonContext.Default.EntityResponseJson,
            Query(query),
            cancellationToken
        );
    }

    /// <summary>
    /// One hop out from an entity (<c>GET /entities/{type}/{name}/neighbourhood</c>).
    /// </summary>
    /// <remarks>
    /// Each neighbour carries its own <c>factCount</c>. Paginated over the edge's own <c>(createdAt, id)</c>,
    /// never over the fact count, which moves under ingest.
    /// </remarks>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="options">The filter and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<NeighbourhoodResponseJson> NeighboursAsync(
        string entityType,
        string name,
        NeighbourhoodOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchNeighboursAsync(entityType, name, options, cancellationToken)
                .ConfigureAwait(false) ?? throw EmptyBody("neighbours");
    }

    /// <summary>
    /// Every neighbour of an entity, following cursors to exhaustion.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="minFacts">Only neighbours with at least this many facts.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="max">Stops the walk once this many neighbours are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<NeighbourJson>> AllNeighboursAsync(
        string entityType,
        string name,
        int? minFacts = null,
        int? limit = null,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchNeighboursAsync(
                    entityType,
                    name,
                    new NeighbourhoodOptions
                    {
                        MinFacts = minFacts,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Neighbours,
            "neighbours",
            max,
            cancellationToken
        );
    }

    /// <summary>
    /// One page of every key's supersession chain for an entity, newest first
    /// (<c>GET /entities/{type}/{name}/history</c>).
    /// </summary>
    /// <remarks>
    /// Answers what changed about the subject; <see cref="HistoryAsync"/> answers how one value changed.
    /// Superseded rows are included: the chain is the point.
    /// </remarks>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<EntityHistoryAllResponseJson> ChangesAsync(
        string entityType,
        string name,
        PageOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchChangesAsync(entityType, name, options, cancellationToken)
                .ConfigureAwait(false) ?? throw EmptyBody("history");
    }

    /// <summary>
    /// The subject's whole change history, following cursors to exhaustion.
    /// </summary>
    /// <remarks>
    /// Unbounded by construction: an attribute revised on every sync has an unbounded chain.
    /// Pass <paramref name="max"/> to stop the walk once that many rows are in hand.
    /// </remarks>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="max">Stops the walk once this many rows are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<AttributeDetailJson>> AllChangesAsync(
        string entityType,
        string name,
        int? limit = null,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchChangesAsync(
                    entityType,
                    name,
                    new PageOptions { Limit = limit, Cursor = cursor },
                    token
                ),
            page => page.Page,
            page => page.History,
            "history",
            max,
            cancellationToken
        );
    }

    /// <summary>
    /// The supersession history for one attribute key.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="key">The attribute key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<List<AttributeDetailJson>> HistoryAsync(
        string entityType,
        string name,
        string key,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(key);
        var response = await _transport
            .RequestRequiredAsync(
                HttpMethod.Get,
                $"{EntityPath(entityType, name)}/history/{AgentMemoryPaths.EncodePathSegment(key)}",
                AgentMemoryJsonContext.Default.EntityHistoryResponseJson,
                null,
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.History;
    }

    /// <summary>
    /// Soft-deletes an entity (sets its valid-until).
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task DeleteAsync(
        string entityType,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        return _transport.SendAsync(
            HttpMethod.Delete,
            EntityPath(entityType, name),
            null,
            cancellationToken
        );
    }

    private string EntityPath(string entityType, string name)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(name);
        return $"{_base}/{AgentMemoryPaths.EncodePathSegment(entityType)}/{AgentMemoryPaths.EncodePathSegment(name)}";
    }

    private Task<EntityListResponseJson?> FetchListAsync(
        EntityListOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters().Add("type", options?.Type).AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.EntityListResponseJson,
            Query(query),
            cancellationToken
        );
    }

    private Task<NeighbourhoodResponseJson?> FetchNeighboursAsync(
        string entityType,
        string name,
        NeighbourhoodOptions? options,
        CancellationToken cancellationToken
    )
    {
        options?.Validate();

        var query = new QueryParameters().Add("minFacts", options?.MinFacts).AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{EntityPath(entityType, name)}/neighbourhood",
            AgentMemoryJsonContext.Default.NeighbourhoodResponseJson,
            Query(query),
            cancellationToken
        );
    }

    private Task<EntityHistoryAllResponseJson?> FetchChangesAsync(
        string entityType,
        string name,
        PageOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters().AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{EntityPath(entityType, name)}/history",
            AgentMemoryJsonContext.Default.EntityHistoryAllResponseJson,
            Query(query),
            cancellationToken
        );
    }
}
