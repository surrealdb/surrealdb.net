using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// The filters <c>/documents</c> accepts, beside its pagination parameters.
/// </summary>
public class DocumentFilters
{
    /// <summary>Only documents in this pipeline status (see <see cref="DocumentStatuses"/>).</summary>
    public string? Status { get; set; }

    /// <summary>Only documents of this MIME type.</summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="Documents.ListAsync"/>.
/// </summary>
public sealed class DocumentListOptions : CursorOrOffsetOptions
{
    /// <summary>Only documents in this pipeline status (see <see cref="DocumentStatuses"/>).</summary>
    public string? Status { get; set; }

    /// <summary>Only documents of this MIME type.</summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// The filters <c>/documents/keywords</c> accepts, beside its pagination parameters.
/// </summary>
public class KeywordFilters
{
    /// <summary>Case-insensitive substring match on the keyword text.</summary>
    public string? Q { get; set; }

    /// <summary>Minimum number of linked documents.</summary>
    public int? MinDocumentCount { get; set; }

    /// <summary>Sort key. A cursor is only valid for the sort it was minted under.</summary>
    public string? Sort { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="DocumentKeywords.ListAsync"/>.
/// </summary>
public sealed class KeywordListOptions : CursorOrOffsetOptions
{
    /// <summary>Case-insensitive substring match on the keyword text.</summary>
    public string? Q { get; set; }

    /// <summary>Minimum number of linked documents.</summary>
    public int? MinDocumentCount { get; set; }

    /// <summary>Sort key. A cursor is only valid for the sort it was minted under.</summary>
    public string? Sort { get; set; }
}

/// <summary>
/// Options for <see cref="Documents.QueryAsync"/>: the wire request, except that <see cref="Lens"/>
/// takes a <see cref="Scope"/> and is normalized to <c>ScopeSets</c>.
/// </summary>
public sealed class DocumentQueryOptions
{
    /// <summary>The query text.</summary>
    public required string Query { get; set; }

    /// <summary>Retrieval mode (see <see cref="QueryModes"/>).</summary>
    public string? Mode { get; set; }

    /// <summary>Max hits to return (default 10, max 50).</summary>
    public int? K { get; set; }

    /// <summary>
    /// Minimum similarity for the vector leg (default 0.5). BM25 hits enter the hybrid modes without it,
    /// so outside <c>vector</c> mode a returned chunk need not reach this score.
    /// </summary>
    public double? Threshold { get; set; }

    /// <summary>Read lens: a DNF scope selector that narrows the read region (outer OR, inner AND).</summary>
    public Scope? Lens { get; set; }

    /// <summary>Restricts the search to some document ids or MIME types.</summary>
    public QueryFilter? Filter { get; set; }

    /// <summary>Geographic filter applied at read time over the chunk locations.</summary>
    public DocGeoFilterJson? Location { get; set; }

    /// <summary>
    /// Opts into sub-question decomposition: one retrieval per sub-question, RRF-merged.
    /// Requires the context to have an LLM provider attached.
    /// </summary>
    public bool? DecomposeQuery { get; set; }

    /// <summary>Graph expansion.</summary>
    public bool? ExpandGraph { get; set; }

    /// <summary>Graph weight.</summary>
    public double? GraphAlpha { get; set; }

    /// <summary>Graph expansion depth.</summary>
    public int? GraphDepth { get; set; }

    /// <summary>Graph edge kinds.</summary>
    public IEnumerable<GraphEdgeKind>? GraphEdges { get; set; }

    /// <summary>RRF constant.</summary>
    public double? RrfK { get; set; }

    /// <summary>
    /// Opts into HyDE (hypothetical document embeddings). Requires the context to have an LLM provider attached.
    /// </summary>
    public bool? UseHyde { get; set; }

    /// <summary>
    /// Opts into cross-encoder re-ranking of the top K. Requires the server to have a re-ranker provider attached.
    /// </summary>
    public bool? UseReranker { get; set; }

    /// <summary>Vector weight.</summary>
    public double? VectorWeight { get; set; }
}

/// <summary>
/// Options for <see cref="DocumentKeywords.SearchAsync"/>.
/// </summary>
public sealed class KeywordSearchOptions
{
    /// <summary>The query text.</summary>
    public required string Query { get; set; }

    /// <summary>Maximum hits to return (default 10, max 50).</summary>
    public int? K { get; set; }

    /// <summary>Minimum similarity a hit must reach.</summary>
    public double? Threshold { get; set; }

    /// <summary>Read lens: a DNF scope selector that narrows the read region (outer OR, inner AND).</summary>
    public Scope? Lens { get; set; }
}

/// <summary>
/// Options for <see cref="Documents.UploadAsync"/> and <see cref="Documents.ReprocessAsync"/>.
/// </summary>
public sealed class DocumentUploadOptions
{
    /// <summary>The file content.</summary>
    public required AgentMemoryFile File { get; set; }

    /// <summary>MIME type of the file part. Defaults to <c>application/octet-stream</c>.</summary>
    public string? ContentType { get; set; }

    /// <summary>File name of the multipart <c>file</c> part. Defaults to the file's own name, or <c>upload</c>.</summary>
    public string? FileName { get; set; }

    /// <summary>Human-readable document title, recorded in the <c>metadata</c> part.</summary>
    public string? Title { get; set; }

    /// <summary>Source label for the document, recorded in the <c>metadata</c> part.</summary>
    public string? Source { get; set; }

    /// <summary>DNF scope selector tagging the document (outer OR, inner AND).</summary>
    public Scope? Scopes { get; set; }

    /// <summary>Descriptive <c>key=value</c> labels stamped onto the document and its chunks.</summary>
    public IEnumerable<string>? Labels { get; set; }

    /// <summary>
    /// Observes the request body being sent, so a large upload can show real progress.
    /// </summary>
    public IProgress<UploadProgress>? UploadProgress { get; set; }

    /// <summary>
    /// Deadline for the upload. Uploads have no deadline by default, because a large body can
    /// legitimately take longer than any fixed one.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// File content for a document upload.
/// </summary>
public sealed class AgentMemoryFile
{
    private readonly Func<CancellationToken, Task<HttpContent>> _createContent;

    private AgentMemoryFile(
        Func<CancellationToken, Task<HttpContent>> createContent,
        string? fileName
    )
    {
        _createContent = createContent;
        FileName = fileName;
    }

    /// <summary>
    /// The file's own name, used for the multipart part when no other name is given.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Content from bytes in memory.
    /// </summary>
    /// <param name="bytes">The content.</param>
    public static AgentMemoryFile FromBytes(ReadOnlyMemory<byte> bytes)
    {
        return new AgentMemoryFile(
            _ => Task.FromResult<HttpContent>(new ReadOnlyMemoryContent(bytes)),
            null
        );
    }

    /// <summary>
    /// Content read from a stream. The stream is read to its end and buffered, and it is not disposed.
    /// </summary>
    /// <param name="stream">The content.</param>
    public static AgentMemoryFile FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new AgentMemoryFile(
            async cancellationToken =>
            {
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                return new ByteArrayContent(buffer.ToArray());
            },
            null
        );
    }

    /// <summary>
    /// Content streamed from a file on disk. The file name defaults to the path's file name.
    /// </summary>
    /// <param name="path">The file path.</param>
    public static AgentMemoryFile FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return new AgentMemoryFile(
            _ =>
                Task.FromResult<HttpContent>(
                    new StreamContent(
                        new FileStream(
                            path,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            bufferSize: 81920,
                            useAsync: true
                        )
                    )
                ),
            Path.GetFileName(path)
        );
    }

    /// <summary>
    /// Converts bytes in memory to file content.
    /// </summary>
    /// <param name="bytes">The content.</param>
    public static implicit operator AgentMemoryFile(byte[] bytes)
    {
        return FromBytes(bytes);
    }

    /// <summary>
    /// Converts bytes in memory to file content.
    /// </summary>
    /// <param name="bytes">The content.</param>
    public static implicit operator AgentMemoryFile(ReadOnlyMemory<byte> bytes)
    {
        return FromBytes(bytes);
    }

    /// <summary>
    /// Converts a stream to file content. The stream is buffered and not disposed.
    /// </summary>
    /// <param name="stream">The content.</param>
    public static implicit operator AgentMemoryFile(Stream stream)
    {
        return FromStream(stream);
    }

    internal Task<HttpContent> CreateContentAsync(CancellationToken cancellationToken)
    {
        return _createContent(cancellationToken);
    }
}
