using System.Net.Http.Headers;
using System.Text;
using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using static SurrealDb.AgentMemory.TransportExtensions;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Document ingestion, retrieval, and corpus search.
/// </summary>
public sealed class Documents
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _base;

    internal Documents(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _base = $"{AgentMemoryPaths.GetContextApiPrefix(contextId)}/documents";
        Keywords = new DocumentKeywords(transport, contextId);
    }

    /// <summary>
    /// The keyword graph for the document corpus.
    /// </summary>
    public DocumentKeywords Keywords { get; }

    /// <summary>
    /// Uploads a document (multipart). Returns the ingestion handle.
    /// </summary>
    /// <param name="options">The file and its metadata.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    public async Task<UploadResponse> UploadAsync(
        DocumentUploadOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        var request = await BuildUploadAsync(options, cancellationToken).ConfigureAwait(false);
        return await _transport
            .RequestRequiredAsync(
                HttpMethod.Post,
                _base,
                AgentMemoryJsonContext.Default.UploadResponse,
                request,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Reprocesses an existing document with replacement bytes (multipart).
    /// </summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="options">The replacement file and its metadata.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    public async Task<UploadResponse> ReprocessAsync(
        string documentId,
        DocumentUploadOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(options);

        var request = await BuildUploadAsync(options, cancellationToken).ConfigureAwait(false);
        var response = await _transport
            .RequestJsonAsync(
                HttpMethod.Put,
                $"{_base}/{AgentMemoryPaths.EncodePathSegment(documentId)}",
                AgentMemoryJsonContext.Default.UploadResponse,
                request,
                cancellationToken
            )
            .ConfigureAwait(false);

        // An empty body means the document was accepted for reprocessing.
        return response
            ?? new UploadResponse(
                contentHash: string.Empty,
                deduplicated: false,
                id: documentId,
                status: DocumentStatus.Queued
            );
    }

    /// <summary>
    /// Metadata for one document.
    /// </summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<DocumentJson> GetAsync(
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(documentId);
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(documentId)}",
            AgentMemoryJsonContext.Default.DocumentJson,
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// The document's original bytes.
    /// </summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="timeout">
    /// The deadline, replacing the client's default. A download is bounded by its size rather than by a round trip,
    /// so a large one can legitimately outlast the default.
    /// </param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<byte[]> RawAsync(
        string documentId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(documentId);
        return _transport.RequestBytesAsync(
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(documentId)}/raw",
            null,
            timeout,
            cancellationToken
        );
    }

    /// <summary>
    /// Lists one page of a document's text chunks, in document order.
    /// </summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="options">Pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<ChunkPageJson> ChunksAsync(
        string documentId,
        CursorOrOffsetOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchChunksAsync(documentId, options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("chunks");
    }

    /// <summary>
    /// Every chunk of a document, following cursors to exhaustion.
    /// </summary>
    /// <remarks>
    /// Reconstructing a document's text needs all of it, and the page size is capped at 500, so a single wide page
    /// silently truncates anything longer. Pass <paramref name="max"/> when only the first chunks are rendered.
    /// </remarks>
    /// <param name="documentId">The document id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="max">Stops the walk once this many chunks are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<ChunkJson>> AllChunksAsync(
        string documentId,
        int? limit = null,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchChunksAsync(
                    documentId,
                    new CursorOrOffsetOptions { Limit = limit, Cursor = cursor },
                    token
                ),
            page => page.Page,
            page => page.Chunks,
            "chunks",
            max,
            cancellationToken
        );
    }

    /// <summary>
    /// Lists one page of documents.
    /// </summary>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<DocumentPageJson> ListAsync(
        DocumentListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("documents");
    }

    /// <summary>
    /// Every matching document, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<DocumentJson>> ListAllAsync(
        DocumentFilters? filters = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(
                    new DocumentListOptions
                    {
                        Status = filters?.Status,
                        MimeType = filters?.MimeType,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Documents,
            "documents",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// How many documents match, without fetching them.
    /// </summary>
    /// <remarks>
    /// Asks for a single row with <c>count</c>, so the total is the only thing paid for beyond one page bound.
    /// </remarks>
    /// <param name="filters">Filters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<long> CountAsync(
        DocumentFilters? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        var page = await ListAsync(
                new DocumentListOptions
                {
                    Status = filters?.Status,
                    MimeType = filters?.MimeType,
                    Limit = 1,
                    Count = true,
                },
                cancellationToken
            )
            .ConfigureAwait(false);
        return page.Page.TotalSize ?? page.Documents.Count;
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        return _transport.SendAsync(
            HttpMethod.Delete,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(documentId)}",
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// Hybrid, vector, BM25 or graph search over the document corpus.
    /// </summary>
    /// <remarks>
    /// The lens narrows the read region for this query only. It can only narrow: the caller's grants still apply,
    /// so a lens outside the granted region yields no hits rather than a <c>403</c>.
    /// </remarks>
    /// <param name="options">The query.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<QueryResponseJson> QueryAsync(
        DocumentQueryOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        var body = new JsonBody()
            .Add("query", options.Query)
            .Add("mode", options.Mode)
            .Add("k", options.K)
            .Add("threshold", options.Threshold)
            .Add("filter", options.Filter, AgentMemoryJsonContext.Default.QueryFilter)
            .Add("location", options.Location, AgentMemoryJsonContext.Default.DocGeoFilterJson)
            .Add("decomposeQuery", options.DecomposeQuery)
            .Add("expandGraph", options.ExpandGraph)
            .Add("graphAlpha", options.GraphAlpha)
            .Add("graphDepth", options.GraphDepth)
            .AddEach("graphEdges", options.GraphEdges, AgentMemoryJsonContext.Default.GraphEdgeKind)
            .Add("rrfK", options.RrfK)
            .Add("useHyde", options.UseHyde)
            .Add("useReranker", options.UseReranker)
            .Add("vectorWeight", options.VectorWeight)
            .Add("lens", options.Lens);

        // A read behind a POST, because the query travels in the body: safe to replay.
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/query",
            AgentMemoryJsonContext.Default.QueryResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Recomputes the derived document-to-keyword and document-to-document links.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<RecomputeLinksResponse> RecomputeLinksAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/recompute-links",
            AgentMemoryJsonContext.Default.RecomputeLinksResponse,
            Json(new JsonBody()),
            cancellationToken
        );
    }

    private Task<ChunkPageJson?> FetchChunksAsync(
        string documentId,
        CursorOrOffsetOptions? options,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(documentId);
        options?.Validate();

        var query = new QueryParameters().AddPage(options).AddOffset(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(documentId)}/chunks",
            AgentMemoryJsonContext.Default.ChunkPageJson,
            Query(query),
            cancellationToken
        );
    }

    private Task<DocumentPageJson?> FetchListAsync(
        DocumentListOptions? options,
        CancellationToken cancellationToken
    )
    {
        options?.Validate();

        var query = new QueryParameters()
            .Add("status", options?.Status)
            .Add("mimeType", options?.MimeType)
            .AddPage(options)
            .AddOffset(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.DocumentPageJson,
            Query(query),
            cancellationToken
        );
    }

    private static async Task<AgentMemoryRequestOptions> BuildUploadAsync(
        DocumentUploadOptions options,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(options.File);

        var form = new MultipartFormDataContent();
        try
        {
            // The server reads multipart fields in declaration order, so the metadata part must precede the file part.
            var metadata = new JsonBody()
                .Add("title", options.Title)
                .Add("source", options.Source)
                .Add("scopes", options.Scopes)
                .Add("labels", options.Labels);
            if (!metadata.IsEmpty)
            {
                var part = new ByteArrayContent(
                    Encoding.UTF8.GetBytes(
                        metadata.Build().ToJsonString(JsonBody.SerializerOptions)
                    )
                );
                part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"metadata\"",
                };
                form.Add(part);
            }

            var file = await options
                .File.CreateContentAsync(cancellationToken)
                .ConfigureAwait(false);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(
                options.ContentType ?? "application/octet-stream"
            );
            file.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = Quote(options.FileName ?? options.File.FileName ?? "upload"),
            };
            form.Add(file);
        }
        catch
        {
            form.Dispose();
            throw;
        }

        return new AgentMemoryRequestOptions
        {
            Content = form,
            Timeout = options.Timeout,
            UploadProgress = options.UploadProgress,
        };
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}

/// <summary>
/// The keyword graph for the document corpus.
/// </summary>
public sealed class DocumentKeywords
{
    private readonly AgentMemoryTransport _transport;
    private readonly string _contextPrefix;
    private readonly string _base;

    internal DocumentKeywords(AgentMemoryTransport transport, string contextId)
    {
        _transport = transport;
        _contextPrefix = AgentMemoryPaths.GetContextApiPrefix(contextId);
        _base = $"{_contextPrefix}/documents/keywords";
    }

    /// <summary>
    /// Lists one page of keywords.
    /// </summary>
    /// <param name="options">Filters and pagination.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<KeywordPageJson> ListAsync(
        KeywordListOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await FetchListAsync(options, cancellationToken).ConfigureAwait(false)
            ?? throw EmptyBody("keywords");
    }

    /// <summary>
    /// Every matching keyword, following cursors to exhaustion.
    /// </summary>
    /// <param name="filters">Filters.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public Task<List<KeywordJson>> ListAllAsync(
        KeywordFilters? filters = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        return AgentMemoryPagination.CollectPagesAsync(
            (cursor, token) =>
                FetchListAsync(
                    new KeywordListOptions
                    {
                        Q = filters?.Q,
                        MinDocumentCount = filters?.MinDocumentCount,
                        Sort = filters?.Sort,
                        Limit = limit,
                        Cursor = cursor,
                    },
                    token
                ),
            page => page.Page,
            page => page.Keywords,
            "keywords",
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Vector search over keyword embeddings.
    /// </summary>
    /// <remarks>
    /// The lens narrows the read region for this search only. It can only narrow: the caller's grants still apply,
    /// so a lens outside the granted region yields no hits rather than a <c>403</c>.
    /// </remarks>
    /// <param name="options">The search.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<KeywordSearchResponseJson> SearchAsync(
        KeywordSearchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        var body = new JsonBody()
            .Add("query", options.Query)
            .Add("k", options.K)
            .Add("threshold", options.Threshold)
            .Add("lens", options.Lens);

        // A read behind a POST, because the query travels in the body: safe to replay.
        return _transport.RequestRequiredAsync(
            HttpMethod.Post,
            $"{_base}/search",
            AgentMemoryJsonContext.Default.KeywordSearchResponseJson,
            Json(body, idempotent: true),
            cancellationToken
        );
    }

    /// <summary>
    /// Gets one keyword by its normalized form.
    /// </summary>
    /// <param name="normalized">The normalized keyword.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<KeywordDetailJson> GetAsync(
        string normalized,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(normalized);
        return _transport.RequestRequiredAsync(
            HttpMethod.Get,
            $"{_base}/{AgentMemoryPaths.EncodePathSegment(normalized)}",
            AgentMemoryJsonContext.Default.KeywordDetailJson,
            null,
            cancellationToken
        );
    }

    /// <summary>
    /// The keywords linked to a document.
    /// </summary>
    /// <param name="documentId">The document id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<List<DocumentKeywordJson>> ForDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(documentId);
        var response = await _transport
            .RequestRequiredAsync(
                HttpMethod.Get,
                $"{_contextPrefix}/documents/{AgentMemoryPaths.EncodePathSegment(documentId)}/keywords",
                AgentMemoryJsonContext.Default.DocumentKeywordsResponse,
                null,
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Keywords;
    }

    private Task<KeywordPageJson?> FetchListAsync(
        KeywordListOptions? options,
        CancellationToken cancellationToken
    )
    {
        options?.Validate();

        var query = new QueryParameters()
            .Add("q", options?.Q)
            .Add("minDocumentCount", options?.MinDocumentCount)
            .Add("sort", options?.Sort)
            .AddPage(options)
            .AddOffset(options);
        return _transport.RequestJsonAsync(
            HttpMethod.Get,
            _base,
            AgentMemoryJsonContext.Default.KeywordPageJson,
            Query(query),
            cancellationToken
        );
    }
}
