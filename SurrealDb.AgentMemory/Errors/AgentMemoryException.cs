using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Base class for every error the Agent Memory client raises.
/// </summary>
/// <remarks>
/// API failures carry the RFC 7807 problem-details fields the service returned.
/// Failures that never produced an HTTP status (a network error, a timeout, a failed stream)
/// report <see cref="Status"/> <c>0</c>.
/// A caller's own cancellation is not an error: it surfaces as <see cref="OperationCanceledException"/>.
/// </remarks>
public class AgentMemoryException : Exception
{
    private const string DefaultTitle = "Agent Memory request failed";

    private static readonly HashSet<string> ReservedFields =
    [
        "status",
        "title",
        "detail",
        "type",
        "instance",
        "message",
    ];

    /// <summary>
    /// Creates an Agent Memory error.
    /// </summary>
    /// <param name="status">HTTP status code, or <c>0</c> when no response was received.</param>
    /// <param name="title">Short error title.</param>
    /// <param name="detail">Human-readable detail.</param>
    /// <param name="type">RFC 7807 <c>type</c> URI.</param>
    /// <param name="instance">RFC 7807 <c>instance</c>.</param>
    /// <param name="extensions">Additional problem-details fields.</param>
    /// <param name="innerException">The underlying cause.</param>
    public AgentMemoryException(
        int status,
        string title,
        string? detail = null,
        string? type = null,
        string? instance = null,
        IReadOnlyDictionary<string, JsonElement>? extensions = null,
        Exception? innerException = null
    )
        : base(FormatMessage(status, title, detail), innerException)
    {
        Status = status;
        Title = title;
        Detail = string.IsNullOrEmpty(detail) ? null : detail;
        Type = type;
        Instance = instance;
        Extensions = extensions ?? new Dictionary<string, JsonElement>();
    }

    /// <summary>
    /// HTTP status code, or <c>0</c> for failures that never produced a response.
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// Short error title from the API, or a generic label.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Human-readable detail, when provided.
    /// </summary>
    public string? Detail { get; }

    /// <summary>
    /// RFC 7807 <c>type</c> URI, when provided.
    /// </summary>
    public string? Type { get; }

    /// <summary>
    /// RFC 7807 <c>instance</c>, when provided.
    /// </summary>
    public string? Instance { get; }

    /// <summary>
    /// Additional problem-details fields.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement> Extensions { get; }

    /// <summary>
    /// Builds a typed error from an API error response.
    /// </summary>
    /// <param name="status">HTTP status code.</param>
    /// <param name="body">The raw response body, or <see langword="null"/>.</param>
    /// <param name="headers">Response headers, read for <c>Retry-After</c> on a 429.</param>
    /// <returns>
    /// <see cref="AgentMemoryValidationException"/> for 400 and 422,
    /// <see cref="AgentMemoryAuthException"/> for 401,
    /// <see cref="AgentMemoryScopeException"/> for 403,
    /// <see cref="AgentMemoryNotFoundException"/> for 404,
    /// <see cref="AgentMemoryRateLimitException"/> for 429,
    /// <see cref="AgentMemoryServerException"/> for 5xx,
    /// and <see cref="AgentMemoryException"/> otherwise.
    /// </returns>
    public static AgentMemoryException FromResponse(
        int status,
        string? body,
        HttpResponseHeaders? headers = null
    )
    {
        var title = DefaultTitle;
        string? detail = null;
        string? type = null;
        string? instance = null;
        var extensions = new Dictionary<string, JsonElement>();

        if (!string.IsNullOrEmpty(body))
        {
            if (TryParse(body, out var root))
            {
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var titleElement =
                        root.TryGetProperty("title", out var t) && t.ValueKind != JsonValueKind.Null
                            ? t
                        : root.TryGetProperty("message", out var m) ? m
                        : default;
                    if (titleElement.ValueKind == JsonValueKind.String)
                    {
                        title = titleElement.GetString()!;
                    }

                    detail = ReadString(root, "detail");
                    type = ReadString(root, "type");
                    instance = ReadString(root, "instance");

                    foreach (var property in root.EnumerateObject())
                    {
                        if (!ReservedFields.Contains(property.Name))
                        {
                            extensions[property.Name] = property.Value.Clone();
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.String)
                {
                    detail = root.GetString();
                }
            }
            else
            {
                detail = body;
            }
        }

        if (status >= 500)
        {
            return new AgentMemoryServerException(
                status,
                title,
                detail,
                type,
                instance,
                extensions
            );
        }

        return status switch
        {
            400 or 422 => new AgentMemoryValidationException(
                status,
                title,
                detail,
                type,
                instance,
                extensions
            ),
            401 => new AgentMemoryAuthException(status, title, detail, type, instance, extensions),
            403 => new AgentMemoryScopeException(status, title, detail, type, instance, extensions),
            404 => new AgentMemoryNotFoundException(
                status,
                title,
                detail,
                type,
                instance,
                extensions
            ),
            429 => new AgentMemoryRateLimitException(
                status,
                title,
                detail,
                type,
                instance,
                extensions,
                ParseRetryAfter(headers)
            ),
            _ => new AgentMemoryException(status, title, detail, type, instance, extensions),
        };
    }

    private static string FormatMessage(int status, string title, string? detail)
    {
        var message = $"[{status.ToString(CultureInfo.InvariantCulture)}] {title}";
        return string.IsNullOrEmpty(detail) ? message : $"{message}: {detail}";
    }

    private static bool TryParse(string body, out JsonElement root)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            root = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            root = default;
            return false;
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static TimeSpan? ParseRetryAfter(HttpResponseHeaders? headers)
    {
        if (headers is null || !headers.TryGetValues("Retry-After", out var values))
        {
            return null;
        }

        // RFC 9110 delay-seconds are non-negative; an HTTP date or a malformed value is not a delay.
        var raw = values.FirstOrDefault();
        if (
            !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || !double.IsFinite(seconds)
            || seconds < 0
        )
        {
            return null;
        }

        try
        {
            return TimeSpan.FromSeconds(seconds);
        }
        catch (OverflowException)
        {
            return TimeSpan.MaxValue;
        }
    }
}
