using System.Text.Json;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Missing or invalid bearer token (401).
/// </summary>
public sealed class AgentMemoryAuthException(
    int status,
    string title,
    string? detail = null,
    string? type = null,
    string? instance = null,
    IReadOnlyDictionary<string, JsonElement>? extensions = null,
    Exception? innerException = null
) : AgentMemoryException(status, title, detail, type, instance, extensions, innerException);

/// <summary>
/// The principal or a scope floor rejected the call (403).
/// </summary>
public sealed class AgentMemoryScopeException(
    int status,
    string title,
    string? detail = null,
    string? type = null,
    string? instance = null,
    IReadOnlyDictionary<string, JsonElement>? extensions = null,
    Exception? innerException = null
) : AgentMemoryException(status, title, detail, type, instance, extensions, innerException);

/// <summary>
/// The resource was not found (404).
/// </summary>
public sealed class AgentMemoryNotFoundException(
    int status,
    string title,
    string? detail = null,
    string? type = null,
    string? instance = null,
    IReadOnlyDictionary<string, JsonElement>? extensions = null,
    Exception? innerException = null
) : AgentMemoryException(status, title, detail, type, instance, extensions, innerException);

/// <summary>
/// The request body or parameters were invalid (400 or 422).
/// </summary>
public sealed class AgentMemoryValidationException(
    int status,
    string title,
    string? detail = null,
    string? type = null,
    string? instance = null,
    IReadOnlyDictionary<string, JsonElement>? extensions = null,
    Exception? innerException = null
) : AgentMemoryException(status, title, detail, type, instance, extensions, innerException);

/// <summary>
/// A rate or token budget was exceeded (429).
/// </summary>
public sealed class AgentMemoryRateLimitException(
    int status,
    string title,
    string? detail = null,
    string? type = null,
    string? instance = null,
    IReadOnlyDictionary<string, JsonElement>? extensions = null,
    TimeSpan? retryAfter = null,
    Exception? innerException = null
) : AgentMemoryException(status, title, detail, type, instance, extensions, innerException)
{
    /// <summary>
    /// The delay from a numeric <c>Retry-After</c> header, when the server sent one.
    /// </summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>
/// The server failed (5xx), after any retries were used up.
/// </summary>
public sealed class AgentMemoryServerException(
    int status,
    string title,
    string? detail = null,
    string? type = null,
    string? instance = null,
    IReadOnlyDictionary<string, JsonElement>? extensions = null,
    Exception? innerException = null
) : AgentMemoryException(status, title, detail, type, instance, extensions, innerException);

/// <summary>
/// A network failure, a timeout, or a response that could not be read (status <c>0</c>).
/// </summary>
/// <remarks>
/// A timeout is reported here, apart from a caller's own cancellation,
/// which surfaces as <see cref="OperationCanceledException"/>: only a timeout describes something that went wrong.
/// </remarks>
public sealed class AgentMemoryConnectionException(
    string title,
    string? detail = null,
    Exception? innerException = null
) : AgentMemoryException(0, title, detail, innerException: innerException);

/// <summary>
/// The server reported a failure part-way through a stream (status <c>0</c>).
/// </summary>
/// <remarks>
/// A streaming chat opens with <c>200</c>, so a failure after the headers arrives as an <c>error</c> frame
/// on the stream itself rather than as an HTTP status.
/// Raising it keeps a mid-flight failure from reading as a stream that simply ended.
/// </remarks>
public sealed class AgentMemoryStreamException(string title, string? detail = null)
    : AgentMemoryException(0, title, detail);
