using System.Runtime.CompilerServices;
using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Typed client for the Agent Memory API: memory writes and recall, document ingestion, sessions, entities,
/// lifecycle, traces, and scope administration.
/// </summary>
/// <remarks>
/// The client is pinned to a single context; every call targets <c>/api/v1/{context}/…</c>.
/// It is safe to share across threads, and it holds nothing that needs disposing.
/// </remarks>
public sealed class AgentMemoryClient
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    /// <summary>
    /// Creates a client that sends its requests through a connection pool shared by every client created this way.
    /// </summary>
    /// <remarks>
    /// The pool recycles its connections every two minutes, so DNS changes are picked up. It keeps no cookies,
    /// because the clients sharing it may hold different API keys, and it does not follow redirects, because a
    /// redirect drops the API key: a redirecting endpoint is reported as an error that names the target.
    /// </remarks>
    /// <param name="options">Context, credentials and endpoint.</param>
    public AgentMemoryClient(AgentMemoryOptions options)
        : this(options, AgentMemoryHttp.Shared) { }

    /// <summary>
    /// Creates a client that sends its requests through <paramref name="httpClient"/>, for example one from
    /// <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <remarks>
    /// Requests go to absolute URLs, so the client needs no <see cref="HttpClient.BaseAddress"/>. Deadlines are
    /// applied per request: give the client an infinite <see cref="HttpClient.Timeout"/>, or large uploads are cut
    /// off by it.
    /// </remarks>
    /// <param name="options">Context, credentials and endpoint.</param>
    /// <param name="httpClient">The HTTP client. The caller owns it.</param>
    public AgentMemoryClient(AgentMemoryOptions options, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);
        options.Validate();

        ContextId = options.Context;
        _transport = new AgentMemoryTransport(
            httpClient,
            options.Endpoint,
            options.ApiKey,
            options.Timeout,
            options.MaxRetries,
            null,
            options.TimeProvider
        );
        _base = AgentMemoryPaths.GetContextApiPrefix(ContextId);

        Documents = new Documents(_transport, ContextId);
        Entities = new Entities(_transport, ContextId);
        Facts = new Facts(_transport, ContextId);
        Uncertainty = new Uncertainty(_transport, ContextId);
        Sessions = new Sessions(_transport, ContextId);
        Lifecycle = new Lifecycle(_transport, ContextId);
        Traces = new Traces(_transport, ContextId);
        Principals = new Principals(_transport, ContextId);
        Scopes = new Scopes(_transport, ContextId);
        Keys = new Keys(_transport, ContextId);
    }

    private AgentMemoryClient(string contextId, AgentMemoryTransport transport)
    {
        ContextId = contextId;
        _transport = transport;
        _base = AgentMemoryPaths.GetContextApiPrefix(contextId);

        Documents = new Documents(transport, contextId);
        Entities = new Entities(transport, contextId);
        Facts = new Facts(transport, contextId);
        Uncertainty = new Uncertainty(transport, contextId);
        Sessions = new Sessions(transport, contextId);
        Lifecycle = new Lifecycle(transport, contextId);
        Traces = new Traces(transport, contextId);
        Principals = new Principals(transport, contextId);
        Scopes = new Scopes(transport, contextId);
        Keys = new Keys(transport, contextId);
    }

    /// <summary>
    /// The Agent Memory context id this client calls.
    /// </summary>
    public string ContextId { get; }

    /// <summary>
    /// The principal every request acts on behalf of, if this client came from <see cref="OnBehalfOf"/>.
    /// </summary>
    public string? OnBehalfOfPrincipal => _transport.OnBehalfOf;

    internal AgentMemoryTransport Transport => _transport;

    /// <summary>
    /// Document ingestion, retrieval, corpus search, and the keyword graph.
    /// </summary>
    public Documents Documents { get; }

    /// <summary>
    /// Entity records, attributes, relations, name search, and attribute history.
    /// </summary>
    public Entities Entities { get; }

    /// <summary>
    /// The attribute, relation, and action collections a bounded head points at.
    /// </summary>
    public Facts Facts { get; }

    /// <summary>
    /// The things the context is unsure about, and the write that settles one.
    /// </summary>
    public Uncertainty Uncertainty { get; }

    /// <summary>
    /// Conversation sessions for this context.
    /// </summary>
    public Sessions Sessions { get; }

    /// <summary>
    /// Expiry and decay sweeps.
    /// </summary>
    public Lifecycle Lifecycle { get; }

    /// <summary>
    /// Retrieval trace tooling.
    /// </summary>
    public Traces Traces { get; }

    /// <summary>
    /// Principals and their scope grants.
    /// </summary>
    public Principals Principals { get; }

    /// <summary>
    /// The scope tree.
    /// </summary>
    public Scopes Scopes { get; }

    /// <summary>
    /// Self-service API keys for this context.
    /// </summary>
    public Keys Keys { get; }

    /// <summary>
    /// Returns a client that issues every request on behalf of <paramref name="principalId"/>, sending the
    /// <c>X-Spectron-On-Behalf-Of</c> delegation header. This client is left unchanged.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The principal must allow delegation. A delegated request runs with the grants that the API key and
    /// the principal both hold. The returned client shares this client's connections.
    /// </para>
    /// <para>
    /// The id is checked before it is used, because the service reads an empty delegation header as no
    /// delegation at all and would run the request with the API key's own grants: a blank id, or
    /// <c>principal:</c> with nothing after it, is refused here.
    /// </para>
    /// </remarks>
    /// <param name="principalId">The principal to act for, with or without the <c>principal:</c> prefix.</param>
    /// <exception cref="ArgumentException">
    /// The id is blank, is only the <c>principal:</c> prefix, has leading or trailing spaces, or contains
    /// control or non-ASCII characters (a line break, for example).
    /// </exception>
    public AgentMemoryClient OnBehalfOf(string principalId)
    {
        return new AgentMemoryClient(ContextId, _transport.WithOnBehalfOf(principalId));
    }

    /// <summary>
    /// Liveness probe for the API (<c>GET /api/v1/health</c>).
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="AgentMemoryException">The service is unhealthy or unreachable.</exception>
    public Task HealthAsync(CancellationToken cancellationToken = default)
    {
        return _transport.SendAsync(HttpMethod.Get, "/api/v1/health", null, cancellationToken);
    }

    /// <summary>
    /// Persists facts from free-form text and/or caller-supplied triples (<c>POST /facts</c>).
    /// Idempotent within a 30-second window, so it is retried safely.
    /// </summary>
    /// <param name="text">The text to extract facts from. Optional when triples are supplied.</param>
    /// <param name="options">Inference mode, session, scopes, labels and triples.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<FactsResponseJson> RememberAsync(
        string? text = null,
        RememberOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonBody()
            .Add("text", text)
            .Add("infer", options?.Infer)
            .Add("session_id", options?.SessionId)
            .Add("scopes", options?.Scopes)
            .Add("role", options?.Role)
            .Add("memory_category", options?.MemoryCategory)
            .Add("labels", options?.Labels)
            .AddEach("triples", options?.Triples, AgentMemoryJsonContext.Default.Triple);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/facts",
            AgentMemoryJsonContext.Default.FactsResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Persists facts from a batch of conversation messages (<c>POST /facts/batch</c>).
    /// Idempotent within a 30-second window, so it is retried safely.
    /// </summary>
    /// <param name="messages">
    /// The messages. A timestamp must carry its offset: use UTC or local time, not
    /// <see cref="DateTimeKind.Unspecified"/>.
    /// </param>
    /// <param name="options">Session, scopes, extraction strategy and labels.</param>
    /// <exception cref="ArgumentException">A message is null, or its timestamp has no kind.</exception>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<FactsBatchResponseJson> RememberManyAsync(
        IEnumerable<BatchMessage> messages,
        RememberManyOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(messages);

        // Enumerated once, so the messages that are checked are the ones that are sent.
        var batch = messages as IReadOnlyCollection<BatchMessage> ?? messages.ToList();
        foreach (var message in batch)
        {
            if (message is null)
            {
                throw new ArgumentException("The batch contains a null message.", nameof(messages));
            }

            if (message.Ts is { Kind: DateTimeKind.Unspecified })
            {
                throw new ArgumentException(
                    "A message timestamp must be UTC or local time. One with DateTimeKind.Unspecified is sent without an offset, and the service refuses the whole batch for it.",
                    nameof(messages)
                );
            }
        }

        var body = new JsonBody()
            .AddEach("messages", batch, AgentMemoryJsonContext.Default.BatchMessage)
            .Add("session_id", options?.SessionId)
            .Add("scopes", options?.Scopes)
            .Add("extract", options?.Extract)
            .Add("infer", options?.Infer)
            .Add("labels", options?.Labels);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/facts/batch",
            AgentMemoryJsonContext.Default.FactsBatchResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Semantic recall over memory for this context (<c>POST /query</c>).
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="options">Hit count, mode, lens, filters and time bounds.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<QueryMemoryResponseJson> RecallAsync(
        string query,
        RecallOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var body = new JsonBody()
            .Add("query", query)
            .Add("k", options?.K)
            .Add("mode", options?.Mode)
            .Add("sessionId", options?.SessionId)
            .Add("include", options?.Include)
            .Add("asOf", options?.AsOf)
            .Add("atInstant", options?.AtInstant)
            .Add("labels", options?.Labels)
            .Add("lens", options?.Lens)
            .Add("scopeView", options?.ScopeView)
            .Add("validFrom", options?.ValidFrom)
            .Add("validUntil", options?.ValidUntil)
            .Add("source", options?.Source)
            .Add("location", options?.Location, AgentMemoryJsonContext.Default.GeoFilterJson);

        // A read behind a POST, because the query travels in the body: safe to replay.
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/query",
            AgentMemoryJsonContext.Default.QueryMemoryResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Forgets memory matching a natural-language query (<c>POST /forget</c>).
    /// </summary>
    /// <param name="query">What to forget.</param>
    /// <param name="options">Whether to purge the supersession history too.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ForgetResponseJson> ForgetAsync(
        string query,
        ForgetOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var body = new JsonBody().Add("query", query).AddFlag("purge", options?.Purge);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/forget",
            AgentMemoryJsonContext.Default.ForgetResponseJson,
            Json(body),
            cancellationToken
        );
    }

    /// <summary>
    /// A full chat round trip (<c>POST /chat</c>): the reply plus what the turn wrote to memory.
    /// </summary>
    /// <param name="message">The user message.</param>
    /// <param name="options">Session, scopes, model and labels.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ChatResponseJson> ChatAsync(
        string message,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/chat",
            AgentMemoryJsonContext.Default.ChatResponseJson,
            Json(ChatBody(message, options)),
            cancellationToken
        );
    }

    /// <summary>
    /// A streaming chat round trip (<c>POST /chat</c> with <c>stream: true</c>): token deltas as they are produced,
    /// then a terminal frame carrying the full reply, what the turn wrote to memory, and its citations.
    /// </summary>
    /// <remarks>
    /// The stream is not retried and has no deadline; <paramref name="cancellationToken"/> bounds it.
    /// A failure after the stream opened is never mistaken for a reply that ended: an <c>error</c> frame, or a stream
    /// that closes before its terminal frame, raises <see cref="AgentMemoryStreamException"/>, and a dropped
    /// connection raises <see cref="AgentMemoryConnectionException"/>.
    /// </remarks>
    /// <param name="message">The user message.</param>
    /// <param name="options">Session, scopes, model and labels.</param>
    /// <param name="cancellationToken">Cancels the stream.</param>
    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        string message,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var body = ChatBody(message, options).Add("stream", true);
        using var response = await _transport
            .StreamAsync(HttpMethod.Post, $"{_base}/chat", body.Build(), cancellationToken)
            .ConfigureAwait(false);
        await using var stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        await foreach (
            var chunk in ChatStreamParser
                .ParseAsync(stream, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// What this context knows about a subject, in one round trip (<c>POST /lookup</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything returned is a stored row: nothing is generated, nothing is summarized by a model, and an identical
    /// query returns an identical answer. Branch on the resolution kind (<c>entity</c>, <c>topic</c>,
    /// <c>ambiguous</c>, <c>empty</c>) rather than inferring the case from an array length.
    /// </para>
    /// <para>
    /// Every section is bounded and reports <c>truncated</c>; none of them page. To read a section in full, walk its
    /// own collection: facts through <see cref="Facts.AllAttributesAsync"/>, relations through
    /// <see cref="Facts.AllEdgesOfAsync"/>, events through <see cref="Facts.AllActionsAsync"/>, passages through
    /// <see cref="RecallAsync"/>, and unknowns through <see cref="Uncertainty"/>.
    /// </para>
    /// </remarks>
    /// <param name="query">The subject to look up.</param>
    /// <param name="options">Subject, sections and limits.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<LookupResponseJson> LookupAsync(
        string query,
        LookupOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var body = new JsonBody()
            .Add("query", query)
            .Add("subject", options?.Subject)
            .Add("entityType", options?.EntityType)
            .Add("ambiguityMargin", options?.AmbiguityMargin)
            .Add("include", options?.Include)
            .Add("factLimit", options?.FactLimit)
            .Add("relationLimit", options?.RelationLimit)
            .Add("eventLimit", options?.EventLimit)
            .Add("passageLimit", options?.PassageLimit)
            .Add("uncertaintyLimit", options?.UncertaintyLimit);

        // A read behind a POST, because the query travels in the body: safe to replay.
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/lookup",
            AgentMemoryJsonContext.Default.LookupResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Retrieves LLM-facing context text for a query, without a session (<c>POST /context</c>).
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="options">Hit count, lens, labels and subject.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ContextQueryResponseJson> ContextAsync(
        string query,
        ContextOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var body = new JsonBody()
            .Add("query", query)
            .Add("k", options?.K)
            .Add("labels", options?.Labels)
            .Add("lens", options?.Lens)
            .Add("scopeView", options?.ScopeView)
            .Add("subject", options?.Subject);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/context",
            AgentMemoryJsonContext.Default.ContextQueryResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Runs a reflection pass, which may persist attributes when asked to (<c>POST /reflect</c>).
    /// </summary>
    /// <param name="query">What to reflect on.</param>
    /// <param name="options">Whether to persist the result.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ReflectResponseJson> ReflectAsync(
        string query,
        ReflectOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var body = new JsonBody().Add("query", query).Add("persist", options?.Persist ?? false);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/reflect",
            AgentMemoryJsonContext.Default.ReflectResponseJson,
            Json(body),
            cancellationToken
        );
    }

    /// <summary>
    /// Consolidates accumulated observations into durable facts (<c>POST /consolidate</c>).
    /// </summary>
    /// <param name="options">Dry run and limits.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ConsolidateResponseJson> ConsolidateAsync(
        ConsolidateOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonBody()
            .AddFlag("dryRun", options?.DryRun)
            .Add("factLimit", options?.FactLimit)
            .Add("observationLimit", options?.ObservationLimit);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/consolidate",
            AgentMemoryJsonContext.Default.ConsolidateResponseJson,
            Json(body),
            cancellationToken
        );
    }

    /// <summary>
    /// Infers and emits new relation edges between entities (<c>POST /elaborate</c>).
    /// </summary>
    /// <param name="options">Entity, budget, sweep and dry run.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ElaborateResponseJson> ElaborateAsync(
        ElaborateOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonBody()
            .Add("entityRef", options?.EntityRef)
            .Add("budget", options?.Budget)
            .AddFlag("sweep", options?.Sweep)
            .AddFlag("dryRun", options?.DryRun);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/elaborate",
            AgentMemoryJsonContext.Default.ElaborateResponseJson,
            Json(body),
            cancellationToken
        );
    }

    /// <summary>
    /// Runs an integrity check over the memory store (<c>POST /fsck</c>).
    /// </summary>
    /// <param name="options">Check, threshold and result limit.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<FsckReportJson> FsckAsync(
        FsckOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonBody()
            .Add("check", options?.Check)
            .Add("duplicateThreshold", options?.DuplicateThreshold)
            .Add("maxResults", options?.MaxResults);
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/fsck",
            AgentMemoryJsonContext.Default.FsckReportJson,
            Json(body),
            cancellationToken
        );
    }

    /// <summary>
    /// Inspects an entity, attribute, or trace by reference (<c>GET /inspect</c>).
    /// </summary>
    /// <param name="reference">The reference to inspect.</param>
    /// <param name="options">Temporal filters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<InspectResponseJson> InspectAsync(
        string reference,
        TemporalOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(reference);

        var query = new QueryParameters().Add("ref", reference).AddTemporal(options);
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/inspect",
            AgentMemoryJsonContext.Default.InspectResponseJson,
            Query(query),
            cancellationToken
        );
    }

    /// <summary>
    /// Lists one page of audit rows for write and recall activity (<c>GET /audit</c>).
    /// </summary>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<AuditResponseJson> AuditAsync(
        AuditOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchAuditAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("rows");
    }

    /// <summary>
    /// Every matching audit row, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<AuditRowJson>> AuditAllAsync(
        AuditFilters? filters = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchAuditAsync(
                    new AuditOptions
                    {
                        Principal = filters?.Principal,
                        Key = filters?.Key,
                        Kind = filters?.Kind,
                        Since = filters?.Since,
                        Until = filters?.Until,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Rows,
            "rows",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// A structured snapshot of memory state (<c>GET /state</c>).
    /// </summary>
    /// <remarks>
    /// A snapshot, not an export: a composite read over six tables, each bounded by <paramref name="limit"/>
    /// (default 100, max 500), and <c>truncated</c> reports which of them had more rows. Five of those tables have
    /// their own collection to enumerate them completely; <c>instructions</c> has none, so when it is truncated the
    /// omitted rows can only be recovered by raising the limit.
    /// </remarks>
    /// <param name="limit">Rows per table.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<StateResponseJson> StateAsync(
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/state",
            AgentMemoryJsonContext.Default.StateResponseJson,
            Query(new QueryParameters().Add("limit", limit)),
            cancellationToken
        );
    }

    /// <summary>
    /// The static and dynamic profile slices (<c>GET /profile</c>).
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ProfileResponseJson> ProfileAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/profile",
            AgentMemoryJsonContext.Default.ProfileResponseJson,
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// The calling principal's identity and resolved grants (<c>GET /me</c>).
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WhoamiJson> WhoamiAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/me",
            AgentMemoryJsonContext.Default.WhoamiJson,
            null,
            cancellationToken
        );
    }

    private static JsonBody ChatBody(string message, ChatOptions? options)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new JsonBody()
            .Add("message", message)
            .Add("sessionId", options?.SessionId)
            .Add("scopes", options?.Scopes)
            .Add("model", options?.Model)
            .AddFlag("bypassCache", options?.BypassCache)
            .Add("labels", options?.Labels)
            .AddFlag("suppressMarkers", options?.SuppressMarkers);
    }

    private Task<AuditResponseJson?> FetchAuditAsync(
        AuditOptions? options,
        CancellationToken cancellationToken
    )
    {
        var query = new QueryParameters()
            .Add("principal", options?.Principal)
            .Add("key", options?.Key)
            .Add("kind", options?.Kind)
            .Add("since", options?.Since)
            .Add("until", options?.Until)
            .Add("limit", options?.Limit)
            .Add("cursor", options?.Cursor);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{_base}/audit",
            AgentMemoryJsonContext.Default.AuditResponseJson,
            Query(query),
            cancellationToken
        );
    }
}
