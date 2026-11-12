using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SurrealDb.AgentMemory.Tests.Infrastructure;

/// <summary>
/// One request exactly as the handler received it: the .NET counterpart of the JavaScript tests' mocked fetch.
/// </summary>
public sealed class RecordedRequest
{
    private readonly Dictionary<string, string> _headers;

    private RecordedRequest(
        HttpRequestMessage request,
        Dictionary<string, string> headers,
        byte[]? body,
        IReadOnlyList<RecordedPart>? parts
    )
    {
        Method = request.Method.Method;
        Url = request.RequestUri!;
        Content = request.Content;
        _headers = headers;
        Body = body;
        Parts = parts;
    }

    public string Method { get; }

    public Uri Url { get; }

    /// <summary>The URL as sent, for substring checks (percent-encoding preserved).</summary>
    public string UrlText => Url.OriginalString;

    public string Path => Url.AbsolutePath;

    public HttpContent? Content { get; }

    public byte[]? Body { get; }

    public string? BodyText => Body is null ? null : Encoding.UTF8.GetString(Body);

    public JsonNode? Json => Body is null || Body.Length == 0 ? null : JsonNode.Parse(Body);

    public IReadOnlyList<RecordedPart>? Parts { get; }

    public string? Header(string name)
    {
        return _headers.TryGetValue(name, out var value) ? value : null;
    }

    public static async Task<RecordedRequest> CaptureAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in request.Headers)
        {
            headers[key] = string.Join(", ", values);
        }

        byte[]? body = null;
        List<RecordedPart>? parts = null;
        if (request.Content is { } content)
        {
            foreach (var (key, values) in content.Headers)
            {
                headers[key] = string.Join(", ", values);
            }

            if (content is MultipartFormDataContent multipart)
            {
                parts = [];
                foreach (var part in multipart)
                {
                    parts.Add(
                        new RecordedPart(
                            part.Headers.ContentDisposition?.Name?.Trim('"'),
                            part.Headers.ContentDisposition?.FileName?.Trim('"'),
                            part.Headers.ContentType?.ToString(),
                            await part.ReadAsByteArrayAsync(cancellationToken)
                        )
                    );
                }
            }
            else
            {
                // Reading the content is what sends it, so a progress wrapper reports here.
                body = await content.ReadAsByteArrayAsync(cancellationToken);
            }
        }

        return new RecordedRequest(request, headers, body, parts);
    }
}

public sealed record RecordedPart(string? Name, string? FileName, string? ContentType, byte[] Bytes)
{
    public string Text => Encoding.UTF8.GetString(Bytes);
}

/// <summary>
/// A message handler that records every request and answers with a scripted response.
/// </summary>
public sealed class FakeHandler(
    Func<RecordedRequest, CancellationToken, Task<HttpResponseMessage>> respond
) : HttpMessageHandler
{
    private readonly List<RecordedRequest> _requests = [];

    public FakeHandler(Func<RecordedRequest, HttpResponseMessage> respond)
        : this((request, _) => Task.FromResult(respond(request))) { }

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return _requests.ToList();
            }
        }
    }

    public RecordedRequest Last => Requests[^1];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var recorded = await RecordedRequest.CaptureAsync(request, cancellationToken);
        lock (_requests)
        {
            _requests.Add(recorded);
        }

        return await respond(recorded, cancellationToken);
    }

    /// <summary>Always answers with the same JSON body.</summary>
    public static FakeHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new FakeHandler(_ => Responses.Json(json, status));
    }

    /// <summary>Never answers: waits until the request is cancelled or times out.</summary>
    public static FakeHandler Stalled()
    {
        return new FakeHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                throw new InvalidOperationException("unreachable");
            }
        );
    }

    /// <summary>Answers after a delay, or fails as soon as the request is cancelled.</summary>
    public static FakeHandler Slow(TimeSpan delay, string json = """{"ok":true}""")
    {
        return new FakeHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(delay, cancellationToken);
                return Responses.Json(json);
            }
        );
    }
}

public static class Responses
{
    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    public static HttpResponseMessage Text(string text, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(status) { Content = new StringContent(text) };
    }

    public static HttpResponseMessage Empty(HttpStatusCode status = HttpStatusCode.NoContent)
    {
        return new HttpResponseMessage(status) { Content = new ByteArrayContent([]) };
    }

    public static HttpResponseMessage Sse(string body)
    {
        var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    public static HttpResponseMessage Bytes(byte[] bytes)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    }
}

public static class TestClient
{
    public const string Context = "ctx-1";

    public static AgentMemoryClient Create(
        FakeHandler handler,
        Action<AgentMemoryOptions>? configure = null,
        string context = Context
    )
    {
        var options = new AgentMemoryOptions
        {
            Context = context,
            ApiKey = "k",
            Endpoint = "https://api.test",
        };
        configure?.Invoke(options);
        return new AgentMemoryClient(options, new HttpClient(handler));
    }

    public static AgentMemoryTransport Transport(
        FakeHandler handler,
        TimeSpan? timeout = null,
        int? maxRetries = null
    )
    {
        return new AgentMemoryTransport(
            new HttpClient(handler),
            "https://example.test",
            "k",
            timeout,
            maxRetries
        );
    }
}

/// <summary>
/// JSON equality that ignores property order, like the JavaScript tests' <c>toEqual</c>.
/// </summary>
public static class JsonAssert
{
    public static void Equivalent(JsonNode? actual, string expected)
    {
        var expectedNode = JsonNode.Parse(expected);
        if (!JsonNode.DeepEquals(actual, expectedNode))
        {
            throw new InvalidOperationException(
                $"JSON mismatch.{Environment.NewLine}Expected: {expectedNode?.ToJsonString()}{Environment.NewLine}Actual:   {actual?.ToJsonString()}"
            );
        }
    }

    public static void Equivalent(JsonElement actual, string expected)
    {
        Equivalent(JsonNode.Parse(actual.GetRawText()), expected);
    }
}

/// <summary>
/// Records progress reports on the reporting thread, unlike <see cref="Progress{T}"/>.
/// </summary>
public sealed class RecordingProgress<T> : IProgress<T>
{
    private readonly List<T> _reports = [];

    public IReadOnlyList<T> Reports
    {
        get
        {
            lock (_reports)
            {
                return _reports.ToList();
            }
        }
    }

    public void Report(T value)
    {
        lock (_reports)
        {
            _reports.Add(value);
        }
    }
}

/// <summary>
/// A clock fixed at one instant; timers still run on real time.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return now;
    }
}
