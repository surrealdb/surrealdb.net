using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// The fact collections that the bounded reads point at.
/// </summary>
/// <remarks>
/// Every surface that returns a bounded head of facts (<see cref="Entities.GetAsync"/>, the <c>/inspect</c> entity
/// reference, and each section of <see cref="AgentMemoryClient.LookupAsync"/>) reports <c>truncated</c> and points
/// here for the rest. These listings page by cursor in the same newest-first order, so a truncated head is a genuine
/// prefix of the walk rather than a separate ranking.
/// </remarks>
public sealed class Facts
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Facts(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = AgentMemoryPaths.GetContextApiPrefix(contextId);
    }

    /// <summary>
    /// Lists one page of live attributes, newest first (<c>GET /attributes</c>).
    /// </summary>
    /// <remarks>
    /// Live means not superseded and inside its validity window. Superseded values are reachable through
    /// <see cref="Entities.ChangesAsync"/> and <see cref="Entities.HistoryAsync"/>.
    /// </remarks>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<AttributeListResponseJson> AttributesAsync(
        AttributeListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchAttributesAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("attributes");
    }

    /// <summary>
    /// Every matching attribute, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="max">Stops the walk once this many rows are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<AttributeDetailJson>> AllAttributesAsync(
        AttributeFilters? filters = null,
        int? limit = null,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchAttributesAsync(
                    new AttributeListOptions
                    {
                        Entity = filters?.Entity,
                        Key = filters?.Key,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Attributes,
            "attributes",
            max,
            cancellationToken
        );
    }

    /// <summary>
    /// Lists one page of live relation edges, newest first (<c>GET /relations</c>).
    /// </summary>
    /// <remarks>
    /// <c>src</c> and <c>dst</c> filter one direction each. An entity's whole edge set is the union of both walks;
    /// <see cref="AllEdgesOfAsync"/> does that for you.
    /// </remarks>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RelationListResponseJson> RelationsAsync(
        RelationListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchRelationsAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("relations");
    }

    /// <summary>
    /// Every matching relation, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="max">Stops the walk once this many rows are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<RelationDetailJson>> AllRelationsAsync(
        RelationFilters? filters = null,
        int? limit = null,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchRelationsAsync(
                    new RelationListOptions
                    {
                        Src = filters?.Src,
                        Dst = filters?.Dst,
                        Label = filters?.Label,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Relations,
            "relations",
            max,
            cancellationToken
        );
    }

    /// <summary>
    /// Every edge touching an entity, in both directions: outbound first, then inbound.
    /// </summary>
    /// <remarks>
    /// This is what a truncated relation section points at. <c>src</c> and <c>dst</c> are separate filters,
    /// so either walk alone reproduces half the set that <see cref="Entities.GetAsync"/> and
    /// <see cref="AgentMemoryClient.LookupAsync"/> return; this runs both and concatenates them.
    /// </remarks>
    /// <param name="entity">The subject, as <c>&lt;type&gt;/&lt;name&gt;</c>.</param>
    /// <param name="label">Only edges with this label.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walks.</param>
    public async Task<List<RelationDetailJson>> AllEdgesOfAsync(
        string entity,
        string? label = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entity);

        var outbound = AllRelationsAsync(
            new RelationFilters { Src = entity, Label = label },
            limit,
            cancellationToken: cancellationToken
        );
        var inbound = AllRelationsAsync(
            new RelationFilters { Dst = entity, Label = label },
            limit,
            cancellationToken: cancellationToken
        );
        await Task.WhenAll(outbound, inbound).ConfigureAwait(false);

        var edges = new List<RelationDetailJson>(outbound.Result.Count + inbound.Result.Count);
        edges.AddRange(outbound.Result);
        edges.AddRange(inbound.Result);
        return edges;
    }

    /// <summary>
    /// Lists one page of live actions (dated events), newest first by write time (<c>GET /actions</c>).
    /// </summary>
    /// <remarks>
    /// Ordered on write time rather than event time because event time is revisable, and a revision under an
    /// event-time ordering would move a row across page boundaries mid-walk. <c>since</c> and <c>until</c> still
    /// bound the event time.
    /// </remarks>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<ActionListResponseJson> ActionsAsync(
        ActionListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchActionsAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("actions");
    }

    /// <summary>
    /// Every matching action, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="max">Stops the walk once this many rows are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<ActionDetailJson>> AllActionsAsync(
        ActionFilters? filters = null,
        int? limit = null,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchActionsAsync(
                    new ActionListOptions
                    {
                        Actor = filters?.Actor,
                        Verb = filters?.Verb,
                        Since = filters?.Since,
                        Until = filters?.Until,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Actions,
            "actions",
            max,
            cancellationToken
        );
    }

    private Task<AttributeListResponseJson?> FetchAttributesAsync(
        AttributeListOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters()
            .Add("entity", options?.Entity)
            .Add("key", options?.Key)
            .AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{_base}/attributes",
            AgentMemoryJsonContext.Default.AttributeListResponseJson,
            Query(query),
            cancellationToken
        );
    }

    private Task<RelationListResponseJson?> FetchRelationsAsync(
        RelationListOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters()
            .Add("src", options?.Src)
            .Add("dst", options?.Dst)
            .Add("label", options?.Label)
            .AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{_base}/relations",
            AgentMemoryJsonContext.Default.RelationListResponseJson,
            Query(query),
            cancellationToken
        );
    }

    private Task<ActionListResponseJson?> FetchActionsAsync(
        ActionListOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters()
            .Add("actor", options?.Actor)
            .Add("verb", options?.Verb)
            .Add("since", options?.Since)
            .Add("until", options?.Until)
            .AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{_base}/actions",
            AgentMemoryJsonContext.Default.ActionListResponseJson,
            Query(query),
            cancellationToken
        );
    }
}
