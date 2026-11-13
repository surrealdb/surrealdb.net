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
    private const string PrincipalPrefix = "principal:";
    private const int CopyBufferSize = 81920;

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
        ValidateEndpoint(endpoint, nameof(endpoint));
        ValidateApiKey(apiKey, nameof(apiKey));
        ValidateTimeout(timeout, nameof(timeout));
        ValidateMaxRetries(maxRetries, nameof(maxRetries));
        if (onBehalfOf is not null)
        {
            ValidateOnBehalfOf(onBehalfOf, nameof(onBehalfOf));
        }

        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
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

    internal HttpClient HttpClient => _httpClient;

    /// <summary>
    /// Returns a copy of this transport that issues every request on behalf of <paramref name="principalId"/>
    /// (adds the <c>X-Spectron-On-Behalf-Of</c> header). This transport is left unchanged.
    /// </summary>
    /// <param name="principalId">The principal to act for.</param>
    /// <exception cref="ArgumentException">
    /// The id is blank, is only the <c>principal:</c> prefix, or contains characters a header cannot carry.
    /// </exception>
    public AgentMemoryTransport WithOnBehalfOf(string principalId)
    {
        ValidateOnBehalfOf(principalId, nameof(principalId));

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
    /// <param name="timeout">
    /// The deadline, replacing the transport's default: positive, or
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> for none.
    /// </param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="AgentMemoryException">
    /// The request failed, or the body is larger than a byte array can hold.
    /// </exception>
    public async Task<byte[]> RequestBytesAsync(
        string path,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        ValidateTimeout(timeout, nameof(timeout));

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
                // Headers first, so the size is known before any of the body is read.
                response = await _httpClient
                    .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, linked.Token)
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
                    await DelayAsync(schedule, attempt, null, cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }
                throw ConnectionFailed(exception);
            }

            using (response)
            {
                var status = (int)response.StatusCode;
                try
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var text = await response
                            .Content.ReadAsStringAsync(linked.Token)
                            .ConfigureAwait(false);
                        if (AgentMemoryRetry.ShouldRetry("GET", status, attempt, _maxRetries))
                        {
                            var retryAfter = AgentMemoryException.ParseRetryAfter(response.Headers);
                            await DelayAsync(schedule, attempt, retryAfter, cancellationToken)
                                .ConfigureAwait(false);
                            continue;
                        }
                        throw AgentMemoryException.FromResponse(status, text, response.Headers);
                    }

                    return await ReadBytesAsync(response.Content, linked.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    throw TimedOut(deadline, exception);
                }
                catch (Exception exception) when (exception is HttpRequestException or IOException)
                {
                    // Aborting a request makes the read under it fail: that is the caller's
                    // cancellation or the deadline, not a dropped connection.
                    cancellationToken.ThrowIfCancellationRequested();
                    if (linked.IsCancellationRequested)
                    {
                        throw TimedOut(deadline, exception);
                    }

                    // The connection dropped while the body was being read.
                    if (AgentMemoryRetry.ShouldRetry("GET", null, attempt, _maxRetries))
                    {
                        await DelayAsync(schedule, attempt, null, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }
                    throw ConnectionFailed(exception);
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
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);

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
            throw ConnectionFailed(exception);
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
        // Content is sent once: it cannot be replayed. It is disposed on every way out of here,
        // including a request that is refused before anything is sent.
        var content = request?.Content;
        try
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(path);
            ValidateTimeout(request?.Timeout, nameof(AgentMemoryRequestOptions.Timeout));

            var methodName = method.Method.ToUpperInvariant();
            var url = BuildUrl(path, request?.Query);
            var schedule = AgentMemoryRetry.BackoffSchedule(_maxRetries);
            var serializedBody = content is null
                ? request?.Body?.ToJsonString(JsonBody.SerializerOptions)
                : null;

            // A multipart body can legitimately take longer than the default deadline,
            // so only an explicit per-request timeout bounds it.
            var deadline = content is not null ? request?.Timeout : request?.Timeout ?? _timeout;

            var idempotent = request?.Idempotent == true && content is null;

            // Idempotent writes carry a key so retries within a 30-second window collapse
            // to a single server-side effect.
            var idempotencyKey = idempotent
                ? AgentMemoryIdempotency.ComputeKey(
                    methodName,
                    path,
                    serializedBody ?? string.Empty,
                    _timeProvider.GetUtcNow(),
                    _onBehalfOf
                )
                : null;

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

                    // The service can refuse an upload from its headers alone (a wrong key, an unknown
                    // delegation target) and then closes the connection. With this header the client
                    // waits for the go-ahead before it sends the body, so such a refusal arrives as its
                    // status; without it, a large body dies on a failed write and the caller sees a
                    // connection error instead of the reason.
                    message.Headers.ExpectContinue = true;
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
                        await DelayAsync(schedule, attempt, null, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }
                    throw content is null ? ConnectionFailed(exception) : UploadFailed(exception);
                }

                using (response)
                {
                    var status = (int)response.StatusCode;

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

                    if (
                        content is null
                        && ShouldRetryResponse(methodName, status, text, attempt, idempotent)
                    )
                    {
                        var retryAfter = AgentMemoryException.ParseRetryAfter(response.Headers);
                        await DelayAsync(schedule, attempt, retryAfter, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
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
            content?.Dispose();
        }
    }

    private bool ShouldRetryResponse(
        string methodName,
        int status,
        string text,
        int attempt,
        bool idempotent
    )
    {
        if (AgentMemoryRetry.ShouldRetry(methodName, status, attempt, _maxRetries, idempotent))
        {
            return true;
        }

        // An earlier attempt with the same key is still running on the service. Coming back later
        // returns its stored response; giving up here would report a write that may well succeed.
        return idempotent
            && attempt < _maxRetries
            && AgentMemoryRetry.IsStillInProgress(status, text);
    }

    /// <summary>
    /// Reads a response body into one array, without the second copy a buffered response costs.
    /// </summary>
    private static async Task<byte[]> ReadBytesAsync(
        HttpContent content,
        CancellationToken cancellationToken
    )
    {
        var declared = content.Headers.ContentLength;
        if (declared > Array.MaxLength)
        {
            throw TooLarge(declared.Value);
        }

        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            if (declared is { } length)
            {
                var bytes = new byte[length];
                await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
                return bytes;
            }

            // No declared length (a chunked or decompressed body): grow as it arrives.
            using var buffer = new MemoryStream();
            var chunk = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
            try
            {
                int read;
                while (
                    (read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false))
                    > 0
                )
                {
                    if (buffer.Length + read > Array.MaxLength)
                    {
                        throw TooLarge(buffer.Length + read);
                    }
                    buffer.Write(chunk, 0, read);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunk);
            }

            return buffer.ToArray();
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
            // Not validated by the header collection, so the id is checked when the transport is built.
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
        // The path is appended to the configured endpoint, which is the only place the API key is
        // ever sent. An absolute URL here is therefore a mistake, and saying so beats a 404 from a
        // mangled path.
        if (StartsWithScheme(path))
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

    /// <summary>
    /// Whether a path begins with a URI scheme, such as <c>https://</c>. A <c>://</c> further in, for
    /// example in a query value, does not count.
    /// </summary>
    private static bool StartsWithScheme(string path)
    {
        var separator = path.IndexOf("://", StringComparison.Ordinal);
        if (separator <= 0 || !char.IsAsciiLetter(path[0]))
        {
            return false;
        }

        for (var i = 1; i < separator; i++)
        {
            var c = path[i];
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('+' or '-' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private CancellationTokenSource? CreateTimeoutSource(TimeSpan? deadline)
    {
        // Longer than a timer can wait (about 49.7 days) is no deadline at all, so TimeSpan.MaxValue works too.
        if (
            deadline is not { } value
            || value <= TimeSpan.Zero
            || value == Timeout.InfiniteTimeSpan
            || value > MaxDeadline
        )
        {
            return null;
        }

        return new CancellationTokenSource(value, _timeProvider);
    }

    /// <summary>
    /// The longest delay a timer supports.
    /// </summary>
    private static readonly TimeSpan MaxDeadline = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    internal static void ValidateApiKey(string? apiKey, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Agent Memory API key is required.", parameterName);
        }

        // A key read from a file or an environment variable often keeps its trailing newline,
        // which would otherwise fail every request with a FormatException.
        if (apiKey.Any(c => c is < '!' or > '~'))
        {
            throw new ArgumentException(
                "The Agent Memory API key contains whitespace, control or non-ASCII characters. Check for a trailing newline.",
                parameterName
            );
        }
    }

    internal static void ValidateEndpoint(string? endpoint, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Agent Memory endpoint is required.", parameterName);
        }

        // Uri.TryCreate trims its input, so a value that kept its trailing newline would pass the
        // check below and then fail every request.
        if (endpoint.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
        {
            throw new ArgumentException(
                "The Agent Memory endpoint contains whitespace or control characters. Check for a trailing newline.",
                parameterName
            );
        }

        if (
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
        )
        {
            throw new ArgumentException(
                $"Agent Memory endpoint '{endpoint}' must be an absolute http(s) URL.",
                parameterName
            );
        }

        // Request paths are appended to the endpoint, so a query or a fragment would swallow them
        // and every call would reach the endpoint's root instead.
        if (uri.Query.Length > 0 || uri.Fragment.Length > 0)
        {
            throw new ArgumentException(
                $"Agent Memory endpoint '{endpoint}' must not have a query string or a fragment.",
                parameterName
            );
        }
    }

    /// <summary>
    /// Checks a deadline: unset, positive, or <see cref="Timeout.InfiniteTimeSpan"/>. Zero and negative
    /// values are refused rather than read as "no deadline", which is what a spent time budget would
    /// otherwise turn into.
    /// </summary>
    internal static void ValidateTimeout(TimeSpan? timeout, string parameterName)
    {
        if (timeout is { } value && value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "The timeout must be positive, or Timeout.InfiniteTimeSpan for no deadline."
            );
        }
    }

    internal static void ValidateMaxRetries(int? maxRetries, string parameterName)
    {
        if (maxRetries is < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "The retry count cannot be negative."
            );
        }
    }

    /// <summary>
    /// Checks a principal id before it becomes a request header.
    /// </summary>
    internal static void ValidateOnBehalfOf(string? principalId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            throw new ArgumentException("OnBehalfOf requires a principal id.", parameterName);
        }

        // A line break would start a new header, or a new request, and a character outside ASCII
        // cannot be sent in a header at all.
        if (principalId.Any(c => c is < ' ' or > '~'))
        {
            throw new ArgumentException(
                "The principal id contains control or non-ASCII characters. Check for a trailing newline.",
                parameterName
            );
        }

        // The service trims the value, strips a `principal:` prefix, and treats an empty result as no
        // delegation at all: the request would then run with the API key's own grants.
        if (principalId[0] == ' ' || principalId[^1] == ' ')
        {
            throw new ArgumentException(
                "The principal id has leading or trailing spaces.",
                parameterName
            );
        }

        if (string.Equals(principalId, PrincipalPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"'{PrincipalPrefix}' is only a prefix: the id after it is missing, and the service would run the request without delegation.",
                parameterName
            );
        }
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
        TimeSpan? retryAfter,
        CancellationToken cancellationToken
    )
    {
        return Task.Delay(
            AgentMemoryRetry.DelayFor(schedule, attempt, retryAfter),
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

    private static AgentMemoryConnectionException ConnectionFailed(Exception exception)
    {
        return new AgentMemoryConnectionException(
            "Connection failed",
            exception.Message,
            exception
        );
    }

    /// <summary>
    /// A connection failure while an upload was on its way. Once the body is flowing, the service
    /// refuses an upload by answering and closing the connection, and the write that then fails hides
    /// the answer: the detail says so, because "connection failed" alone points the wrong way.
    /// </summary>
    private static AgentMemoryConnectionException UploadFailed(HttpRequestException exception)
    {
        var neverConnected =
            exception.HttpRequestError
                is HttpRequestError.NameResolutionError
                    or HttpRequestError.ConnectionError
                    or HttpRequestError.SecureConnectionError
                    or HttpRequestError.ProxyTunnelError;
        if (neverConnected)
        {
            return ConnectionFailed(exception);
        }

        return new AgentMemoryConnectionException(
            "Connection failed",
            $"{exception.Message} If the connection closed while the body was being sent, the service may have refused the upload part-way (for its media type, scope or labels, for example), and its answer is lost.",
            exception
        );
    }

    private static AgentMemoryException TooLarge(long length)
    {
        return new AgentMemoryException(
            0,
            "Response too large",
            $"The body is {length} bytes, more than a byte array can hold ({Array.MaxLength})."
        );
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
