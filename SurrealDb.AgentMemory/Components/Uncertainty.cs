using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// The things this context is unsure about, and the one write that settles one.
/// </summary>
/// <remarks>
/// <see cref="AgentMemoryClient.StateAsync"/> collapses these to <c>{about, reason}</c>, which is enough to say
/// something is unresolved and not enough to act on it. These rows carry the subject, so a flag can be linked to the
/// entity it is about and settled.
/// </remarks>
public sealed class Uncertainty
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Uncertainty(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/uncertainty";
    }

    /// <summary>
    /// Lists one page of uncertainty flags, newest first (<c>GET /uncertainty</c>).
    /// </summary>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<UncertaintyListResponseJson> ListAsync(
        UncertaintyListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("unknowns");
    }

    /// <summary>
    /// Every matching flag, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<UncertaintyJson>> ListAllAsync(
        UncertaintyFilters? filters = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(
                    new UncertaintyListOptions
                    {
                        Entity = filters?.Entity,
                        Resolved = filters?.Resolved,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Unknowns,
            "unknowns",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// How many flags match, without fetching them.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<long> CountAsync(
        UncertaintyFilters? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        var page = await ListAsync(
                new UncertaintyListOptions
                {
                    Entity = filters?.Entity,
                    Resolved = filters?.Resolved,
                    Limit = 1,
                    Count = true,
                },
                cancellationToken
            )
            .ConfigureAwait(false);
        return page.Page.TotalSize ?? page.Unknowns.Count;
    }

    /// <summary>
    /// Settles a flag by accepting one value (<c>POST /uncertainty/{id}/resolve</c>). Requires the <c>memory:write</c> grant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One call, three effects: the flag is claimed, the accepted value is written through the reconciler at the
    /// upsert trust prior, and the values it beats are retired. The value lands at the flag's own scope, because it
    /// has to replace the contenders where they live.
    /// </para>
    /// <para>
    /// Settlement converges on retry rather than being transactional: repeating a failed call dedups the value and
    /// finishes the retirement. A flag without a recorded entity and key is refused with a <c>422</c>, so check
    /// <c>resolvable</c> on the row before offering the action.
    /// </para>
    /// </remarks>
    /// <param name="uncertaintyId">The flag id.</param>
    /// <param name="acceptedValue">The value to accept.</param>
    /// <param name="options">An optional note explaining the choice.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<UncertaintyJson> ResolveAsync(
        string uncertaintyId,
        string acceptedValue,
        ResolveUncertaintyOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(uncertaintyId);
        ArgumentNullException.ThrowIfNull(acceptedValue);

        var body = new JsonBody().Add("acceptedValue", acceptedValue).Add("note", options?.Note);
        var response = await _transport
            .RequestRequiredAsync(
                HttpMethod.Post,
                $"{_base}/{AgentMemoryPaths.EncodePathSegment(uncertaintyId)}/resolve",
                AgentMemoryJsonContext.Default.ResolveUncertaintyResponseJson,
                Json(body),
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Uncertainty;
    }

    private Task<UncertaintyListResponseJson?> FetchListAsync(
        UncertaintyListOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters()
            .Add("entity", options?.Entity)
            .Add("resolved", options?.Resolved)
            .AddPage(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.UncertaintyListResponseJson,
            Query(query),
            cancellationToken
        );
    }
}
