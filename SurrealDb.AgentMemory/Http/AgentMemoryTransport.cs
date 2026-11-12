using System.Buffers;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Performs authenticated HTTP requests against the Agent Memory API, with retries, idempotency keys,
/// JSON handling, typed errors, and server-sent-event streaming.
/// </summary>
/// <remarks>
/// <see cref="AgentMemoryClient"/> wraps every endpoint. Use the transport directly only to call
/// an endpoint the client does not wrap.
/// </remarks>
public sealed class AgentMemoryTransport
{
    /// <summary>
    /// The delegation header. A wire constant, not branding: the service matches the name exactly
    /// and its CORS allowlist carries only this spelling.
    /// </summary>
    public const string OnBehalfOfHeader = "X-Spectron-On-Behalf-Of";

    /// <summary>
    /// The default request deadline.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The default number of retries for reads and idempotent writes.
    /// </summary>
    public const int DefaultMaxRetries = 3;

    private const string JsonMediaType = "application/json";

    private static readonly string UserAgent = $"surrealdb-memory-dotnet/{GetVersion()}";

    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly string? _onBehalfOf;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a transport that sends its requests through <paramref name="httpClient"/>.
    /// </summary>
    /// <remarks>
    /// Requests are sent to absolute URLs built from <paramref name="endpoint"/>, so the client needs no
    /// <see cref="HttpClient.BaseAddress"/>. Deadlines are applied per request, so a client with an infinite
    /// <see cref="HttpClient.Timeout"/> is recommended; a finite one still applies on top.
    /// </remarks>
    /// <param name="httpClient">The HTTP client. The transport does not dispose it.</param>
    /// <param name="endpoint">API endpoint origin, for example <c>https://memory.example.com</c>.</param>
    /// <param name="apiKey">API key sent as an <c>Authorization: Bearer</c> token.</param>
    /// <param name="timeout">Default request deadline. Defaults to 30 seconds.</param>
    /// <param name="maxRetries">Maximum retries for reads and idempotent writes. Defaults to 3.</param>
    /// <param name="onBehalfOf">Principal id to act on behalf of, sent as <c>X-Spectron-On-Behalf-Of</c>.</param>
    public AgentMemoryTransport(
        HttpClient httpClient,
        string endpoint,
        string apiKey,
        TimeSpan? timeout = null,
        int? maxRetries = null,
        string? onBehalfOf = null
    )
        : this(httpClient, endpoint, apiKey, timeout, maxRetries, onBehalfOf, TimeProvider.System)
    { }

    internal AgentMemoryTransport(
        HttpClient httpClient,
        string endpoint,
        string apiKey,
        TimeSpan? timeout,
        int? maxRetries,
        string? onBehalfOf,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Agent Memory endpoint is required.", nameof(endpoint));
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Agent Memory API key is required.", nameof(apiKey));
        }

        var trimmed = endpoint.TrimEnd('/');
        if (
            !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
        )
        {
            throw new ArgumentException(
                $"Agent Memory endpoint '{endpoint}' must be an absolute http(s) URL.",
                nameof(endpoint)
            );
        }
        if (timeout is { } value && value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The timeout must be positive.");
        }
        if (maxRetries is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRetries),
                "The retry count cannot be negative."
            );
        }

        _httpClient = httpClient;
        _endpoint = trimmed;
        _apiKey = apiKey;
        _timeout = timeout ?? DefaultTimeout;
        _maxRetries = maxRetries ?? DefaultMaxRetries;
        _onBehalfOf = onBehalfOf;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// API endpoint origin, without a trailing slash.
    /// </summary>
    public string Endpoint => _endpoint;

    /// <summary>
    /// The principal this transport acts on behalf of, if any.
    /// </summary>
    public string? OnBehalfOf => _onBehalfOf;

    /// <summary>
    /// Returns a copy of this transport that issues every request on behalf of <paramref name="principalId"/>
    /// (adds the <c>X-Spectron-On-Behalf-Of</c> header). This transport is left unchanged.
    /// </summary>
    /// <param name="principalId">The principal to act for.</param>
    public AgentMemoryTransport WithOnBehalfOf(string principalId)
    {
        if (string.IsNullOrEmpty(principalId))
        {
            throw new ArgumentException("OnBehalfOf requires a principal id.", nameof(principalId));
        }

        return new AgentMemoryTransport(
            _httpClient,
            _endpoint,
            _apiKey,
            _timeout,
            _maxRetries,
            principalId,
            _timeProvider
        );
    }

    /// <summary>
    /// Sends a request and returns its decoded JSON body.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Path below the endpoint, for example <c>/api/v1/my-context/state</c>.</param>
    /// <param name="request">Query, body, deadline and retry options.</param>
    /// <param name="cancellationToken">Cancels the request; surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>
    /// The parsed JSON body; a JSON string holding the raw text when the body is not JSON;
    /// or <see langword="null"/> for an empty body (for example a <c>204</c>).
    /// </returns>
    /// <exception cref="AgentMemoryException">The request failed.</exception>
    public async Task<JsonElement?> RequestJsonAsync(
        HttpMethod method,
        string path,
        AgentMemoryRequestOptions? request = null,
        CancellationToken cancellationToken = default
    )
    {
        var text = await RequestTextAsync(method, path, request, cancellationToken)
            .ConfigureAwait(false);
        if (text is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Not JSON: hand the text back as a JSON string, as the JavaScript client does.
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStringValue(text);
            }

            using var document = JsonDocument.Parse(buffer.WrittenMemory);
            return document.RootElement.Clone();
        }
    }

    /// <summary>
    /// Sends a request and deserializes its JSON body.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Path below the endpoint.</param>
    /// <param name="typeInfo">Metadata for the response type, for example from <see cref="Client.AgentMemoryJsonContext"/>.</param>
    /// <param name="request">Query, body, deadline and retry options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The response, or <see langword="null"/> for an empty body.</returns>
    /// <exception cref="AgentMemoryException">The request failed, or the body did not match <typeparamref name="T"/>.</exception>
    public async Task<T?> RequestJsonAsync<T>(
        HttpMethod method,
        string path,
        JsonTypeInfo<T> typeInfo,
        AgentMemoryRequestOptions? request = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var text = await RequestTextAsync(method, path, request, cancellationToken)
            .ConfigureAwait(false);
        return text is null ? null : Decode(text, typeInfo);
    }

    /// <summary>
    /// Sends a <c>GET</c> that returns raw bytes, such as a document's original file.
    /// </summary>
    /// <param name="path">Path below the endpoint.</param>
    /// <param name="query">Query-string parameters.</param>
    /// <param name="timeout">The deadline, replacing the transport's default.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<byte[]> RequestBytesAsync(
        string path,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        var url = BuildUrl(path, query);
        var deadline = timeout ?? _timeout;
        var schedule = AgentMemoryRetry.BackoffSchedule(_maxRetries);

        cancellationToken.ThrowIfCancellationRequested();

        for (var attempt = 0; ; attempt++)
        {
            using var timeoutSource = CreateTimeoutSource(deadline);
            using var linked = Link(cancellationToken, timeoutSource);
            using var message = CreateMessage(HttpMethod.Get, url, "*/*");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient
                    .SendAsync(message, HttpCompletionOption.ResponseContentRead, linked.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw TimedOut(deadline, exception);
            }
            catch (HttpRequestException exception)
            {
                if (AgentMemoryRetry.ShouldRetry("GET", null, attempt, _maxRetries))
                {
                    await DelayAsync(schedule, attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                throw new AgentMemoryConnectionException(
                    "Connection failed",
                    exception.Message,
                    exception
                );
            }

            using (response)
            {
                var status = (int)response.StatusCode;
                if (
                    status >= 400
                    && AgentMemoryRetry.ShouldRetry("GET", status, attempt, _maxRetries)
                )
                {
                    await DelayAsync(schedule, attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var text = await response
                            .Content.ReadAsStringAsync(linked.Token)
                            .ConfigureAwait(false);
                        throw AgentMemoryException.FromResponse(status, text, response.Headers);
                    }

                    return await response
                        .Content.ReadAsByteArrayAsync(linked.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    throw TimedOut(deadline, exception);
                }
            }
        }
    }

    /// <summary>
    /// Opens a server-sent-event stream, such as a streaming chat.
    /// </summary>
    /// <remarks>
    /// A stream is not retried and has no deadline; the caller's token bounds it.
    /// </remarks>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Path below the endpoint.</param>
    /// <param name="body">A JSON body.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The open response. The caller disposes it.</returns>
    /// <exception cref="AgentMemoryException">The request failed before the stream opened.</exception>
    public async Task<HttpResponseMessage> StreamAsync(
        HttpMethod method,
        string path,
        System.Text.Json.Nodes.JsonNode? body = null,
        CancellationToken cancellationToken = default
    )
    {
        var url = BuildUrl(path, null);
        using var message = CreateMessage(method, url, "text/event-stream");
        if (body is not null)
        {
            message.Content = CreateJsonContent(body.ToJsonString(JsonBody.SerializerOptions));
        }

        cancellationToken.ThrowIfCancellationRequested();

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            // Only the HttpClient's own timeout can end up here.
            throw new AgentMemoryConnectionException(
                "Request timed out",
                exception.Message,
                exception
            );
        }
        catch (HttpRequestException exception)
        {
            throw new AgentMemoryConnectionException(
                "Connection failed",
                exception.Message,
                exception
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                var text = await response
                    .Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                throw AgentMemoryException.FromResponse(
                    (int)response.StatusCode,
                    text,
                    response.Headers
                );
            }
        }

        return response;
    }

    internal T Decode<T>(string text, JsonTypeInfo<T> typeInfo)
    {
        try
        {
            return JsonSerializer.Deserialize(text, typeInfo)
                ?? throw new JsonException("The response body was null.");
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            // The generated models validate required fields and enum values while reading.
            throw new AgentMemoryException(
                0,
                "Unexpected response body",
                $"The response could not be read as {typeof(T).Name}: {exception.Message}",
                innerException: exception
            );
        }
    }

    /// <summary>
    /// Whether an exception is how a body that does not match a model surfaces while it is read.
    /// The generated converters throw a range of types for a missing field, an unknown enum value,
    /// or a value they cannot parse.
    /// </summary>
    internal static bool IsDecodeFailure(Exception exception)
    {
        return exception
            is JsonException
                or ArgumentException
                or InvalidOperationException
                or NotImplementedException
                or NotSupportedException
                or FormatException
                or OverflowException;
    }

    /// <summary>
    /// Sends a request and returns its body as text, or <see langword="null"/> for an empty body.
    /// </summary>
    internal async Task<string?> RequestTextAsync(
        HttpMethod method,
        string path,
        AgentMemoryRequestOptions? request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);

        var methodName = method.Method.ToUpperInvariant();
        var url = BuildUrl(path, request?.Query);
        var schedule = AgentMemoryRetry.BackoffSchedule(_maxRetries);
        var content = request?.Content;
        var serializedBody = content is null
            ? request?.Body?.ToJsonString(JsonBody.SerializerOptions)
            : null;

        // A multipart body can legitimately take longer than the default deadline,
        // so only an explicit per-request timeout bounds it.
        var deadline = content is not null ? request?.Timeout : request?.Timeout ?? _timeout;

        // Content is sent once: it cannot be replayed.
        var idempotent = request?.Idempotent == true && content is null;

        // Idempotent writes carry a key so retries within a 30-second window collapse
        // to a single server-side effect.
        var idempotencyKey = idempotent
            ? AgentMemoryIdempotency.ComputeKey(
                methodName,
                path,
                serializedBody ?? string.Empty,
                _timeProvider.GetUtcNow()
            )
            : null;

        try
        {
            // Already cancelled before it began, so there is nothing worth sending.
            cancellationToken.ThrowIfCancellationRequested();

            for (var attempt = 0; ; attempt++)
            {
                using var timeoutSource = CreateTimeoutSource(deadline);
                using var linked = Link(cancellationToken, timeoutSource);
                using var message = CreateMessage(method, url, JsonMediaType);

                if (content is not null)
                {
                    message.Content = request?.UploadProgress is { } progress
                        ? new ProgressContent(content, progress)
                        : content;
                }
                else if (serializedBody is not null)
                {
                    message.Content = CreateJsonContent(serializedBody);
                }

                if (idempotencyKey is not null)
                {
                    message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
                }

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient
                        .SendAsync(message, HttpCompletionOption.ResponseContentRead, linked.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    // Not the caller's cancellation, so a deadline expired. Never retried.
                    throw TimedOut(deadline, exception);
                }
                catch (HttpRequestException exception)
                {
                    if (
                        content is null
                        && AgentMemoryRetry.ShouldRetry(
                            methodName,
                            null,
                            attempt,
                            _maxRetries,
                            idempotent
                        )
                    )
                    {
                        await DelayAsync(schedule, attempt, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }
                    throw new AgentMemoryConnectionException(
                        "Connection failed",
                        exception.Message,
                        exception
                    );
                }

                using (response)
                {
                    var status = (int)response.StatusCode;
                    if (
                        status >= 400
                        && content is null
                        && AgentMemoryRetry.ShouldRetry(
                            methodName,
                            status,
                            attempt,
                            _maxRetries,
                            idempotent
                        )
                    )
                    {
                        await DelayAsync(schedule, attempt, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    string text;
                    try
                    {
                        text = await response
                            .Content.ReadAsStringAsync(linked.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException exception)
                        when (!cancellationToken.IsCancellationRequested)
                    {
                        throw TimedOut(deadline, exception);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw AgentMemoryException.FromResponse(status, text, response.Headers);
                    }

                    return status == 204 || text.Length == 0 ? null : text;
                }
            }
        }
        finally
        {
            // The request message disposes the content it sent; this covers a request that never got that far.
            content?.Dispose();
        }
    }

    private HttpRequestMessage CreateMessage(HttpMethod method, Uri url, string accept)
    {
        var message = new HttpRequestMessage(method, url);
        message.Headers.Accept.ParseAdd(accept);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        message.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        if (_onBehalfOf is not null)
        {
            message.Headers.TryAddWithoutValidation(OnBehalfOfHeader, _onBehalfOf);
        }
        return message;
    }

    private static ByteArrayContent CreateJsonContent(string json)
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType);
        return content;
    }

    private Uri BuildUrl(string path, IEnumerable<KeyValuePair<string, string?>>? query)
    {
        // Every request carries the API key, so it only ever goes to the configured endpoint:
        // an absolute URL would hand the key to whichever host it names.
        if (path.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The path must be relative to the Agent Memory endpoint, but '{path}' is an absolute URL.",
                nameof(path)
            );
        }

        var builder = new StringBuilder();
        builder.Append(_endpoint).Append('/').Append(path.TrimStart('/'));

        if (query is not null)
        {
            var separator = '?';
            foreach (var (key, value) in query)
            {
                if (value is null)
                {
                    continue;
                }

                builder
                    .Append(separator)
                    .Append(Uri.EscapeDataString(key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(value));
                separator = '&';
            }
        }

        return new Uri(builder.ToString(), UriKind.Absolute);
    }

    private CancellationTokenSource? CreateTimeoutSource(TimeSpan? deadline)
    {
        if (
            deadline is not { } value
            || value <= TimeSpan.Zero
            || value == Timeout.InfiniteTimeSpan
        )
        {
            return null;
        }

        return new CancellationTokenSource(value, _timeProvider);
    }

    private static CancellationTokenSource Link(
        CancellationToken cancellationToken,
        CancellationTokenSource? timeoutSource
    )
    {
        return timeoutSource is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token
            );
    }

    private Task DelayAsync(
        IReadOnlyList<TimeSpan> schedule,
        int attempt,
        CancellationToken cancellationToken
    )
    {
        return Task.Delay(
            AgentMemoryRetry.DelayFor(schedule, attempt),
            _timeProvider,
            cancellationToken
        );
    }

    private static AgentMemoryConnectionException TimedOut(TimeSpan? deadline, Exception exception)
    {
        var detail =
            deadline is { } value && value > TimeSpan.Zero
                ? $"Exceeded {value.TotalMilliseconds:0}ms"
                : exception.Message;
        return new AgentMemoryConnectionException("Request timed out", detail, exception);
    }

    private static string GetVersion()
    {
        var version = typeof(AgentMemoryTransport)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (string.IsNullOrEmpty(version))
        {
            return "0.0.0";
        }

        var metadata = version.IndexOf('+', StringComparison.Ordinal);
        return metadata >= 0 ? version[..metadata] : version;
    }
}
