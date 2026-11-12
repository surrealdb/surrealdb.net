namespace SurrealDb.AgentMemory;

/// <summary>
/// Options for constructing an <see cref="AgentMemoryClient"/>.
/// </summary>
public sealed class AgentMemoryOptions
{
    /// <summary>
    /// Agent Memory context id. Every call targets <c>/api/v1/{Context}/…</c>.
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// API key sent as an <c>Authorization: Bearer</c> token.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint origin, for example <c>https://memory.example.com</c>. A trailing slash is ignored.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Request deadline. Defaults to 30 seconds.
    /// </summary>
    /// <remarks>
    /// Document uploads have no deadline by default, because a large body can legitimately take longer than any
    /// fixed one; pass <see cref="DocumentUploadOptions.Timeout"/> to bound one. Streams are bounded by the
    /// caller's cancellation token only.
    /// </remarks>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Maximum retries for reads and idempotent writes. Defaults to 3.
    /// </summary>
    public int? MaxRetries { get; set; }

    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Context))
        {
            throw new ArgumentException("Agent Memory context is required.", nameof(Context));
        }
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("Agent Memory API key is required.", nameof(ApiKey));
        }
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new ArgumentException("Agent Memory endpoint is required.", nameof(Endpoint));
        }
        if (
            !Uri.TryCreate(Endpoint.TrimEnd('/'), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
        )
        {
            throw new ArgumentException(
                $"Agent Memory endpoint '{Endpoint}' must be an absolute http(s) URL.",
                nameof(Endpoint)
            );
        }
    }
}
