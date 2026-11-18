using System.Text.Json.Nodes;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Per-request options for <see cref="AgentMemoryTransport"/>.
/// </summary>
public sealed class AgentMemoryRequestOptions
{
    /// <summary>
    /// Query-string parameters. Entries with a <see langword="null"/> value are skipped.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string?>>? Query { get; init; }

    /// <summary>
    /// A JSON body, sent as <c>application/json</c>.
    /// </summary>
    public JsonNode? Body { get; init; }

    /// <summary>
    /// A non-JSON body, such as a multipart upload. Takes precedence over <see cref="Body"/>.
    /// </summary>
    /// <remarks>
    /// The transport owns the content and disposes it. A request with content is sent once and never retried,
    /// and it has no deadline unless <see cref="Timeout"/> is set,
    /// because a large body can legitimately take longer than any fixed deadline.
    /// </remarks>
    public HttpContent? Content { get; init; }

    /// <summary>
    /// The deadline for this request, replacing the transport's default.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Marks a write as safe to replay: it carries an <c>Idempotency-Key</c> and takes the retry budget reads get.
    /// </summary>
    public bool Idempotent { get; init; }

    /// <summary>
    /// Observes <see cref="Content"/> being sent.
    /// </summary>
    public IProgress<UploadProgress>? UploadProgress { get; init; }
}
