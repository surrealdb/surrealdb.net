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

            // Reading the content is what sends it, so a progress wrapper reports here.
            body = await content.ReadAsByteArrayAsync(cancellationToken);

            if (
                content.Headers.ContentType is { MediaType: { } mediaType } contentType
                && mediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase)
            )
            {
                var boundary = contentType
                    .Parameters.Single(parameter => parameter.Name == "boundary")
                    .Value!.Trim('"');
                parts = MultipartWire.Parse(body, boundary);
            }
        }

        return new RecordedRequest(request, headers, body, parts);
    }
}

/// <summary>
/// One multipart part, parsed from the bytes that were sent rather than from .NET's object model,
/// so the encoding on the wire is what the tests see.
/// </summary>
public sealed record RecordedPart(
    string Disposition,
    string? Name,
    string? FileName,
    string? ContentType,
    byte[] Bytes
)
{
    public string Text => Encoding.UTF8.GetString(Bytes);
}

internal static class MultipartWire
{
    public static List<RecordedPart> Parse(byte[] body, string boundary)
    {
        // Latin-1 maps every byte to one char, so offsets in the text are offsets in the bytes.
        var text = Encoding.Latin1.GetString(body);
        var delimiter = "--" + boundary;
        var parts = new List<RecordedPart>();

        var position = text.IndexOf(delimiter, StringComparison.Ordinal);
        while (position >= 0)
        {
            var start = position + delimiter.Length;
            if (text.AsSpan(start).StartsWith("--"))
            {
                break;
            }

            start += 2; // CRLF after the delimiter
            var next = text.IndexOf("\r\n" + delimiter, start, StringComparison.Ordinal);
            var headerEnd = text.IndexOf("\r\n\r\n", start, StringComparison.Ordinal);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (
                var line in Encoding
                    .UTF8.GetString(body, start, headerEnd - start)
                    .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            )
            {
                var colon = line.IndexOf(':', StringComparison.Ordinal);
                headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }

            var contentStart = headerEnd + 4;
            var disposition = headers.GetValueOrDefault("Content-Disposition", "");
            parts.Add(
                new RecordedPart(
                    disposition,
                    Parameter(disposition, "name"),
                    Parameter(disposition, "filename"),
                    headers.GetValueOrDefault("Content-Type"),
                    body[contentStart..next]
                )
            );

            position = next + 2;
        }

        return parts;
    }

    private static string? Parameter(string disposition, string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            disposition,
            $"(?:^|;)\\s*{name}=\"([^\"]*)\""
        );
        return match.Success ? match.Groups[1].Value : null;
    }
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
/// A clock whose timers fire at once and are recorded, so a test reads the waits a retry asked for
/// without sitting through them. Use it with an infinite request timeout: a deadline is a timer too.
/// </summary>
public sealed class InstantTimeProvider : TimeProvider
{
    private readonly List<TimeSpan> _delays = [];

    public IReadOnlyList<TimeSpan> Delays
    {
        get
        {
            lock (_delays)
            {
                return _delays.ToList();
            }
        }
    }

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        lock (_delays)
        {
            _delays.Add(dueTime);
        }

        return base.CreateTimer(callback, state, TimeSpan.Zero, period);
    }
}

/// <summary>
/// A stream that cannot seek and serves its content a few bytes at a time, optionally failing part-way:
/// what a network stream looks like to the code that reads it.
/// </summary>
public sealed class FlakyStream(byte[] content, int? failAfter = null, bool failEveryTime = false)
    : Stream
{
    private int _position;
    private bool _failed;

    public int Position32 => _position;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        // Yield, so two readers of one stream really do interleave.
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        if (failAfter is { } limit && _position >= limit && (failEveryTime || !_failed))
        {
            _failed = true;
            throw new IOException("The source stream failed part-way.");
        }

        var bound = failAfter is { } stop && !_failed ? stop : content.Length;
        var count = Math.Min(Math.Min(buffer.Length, 1024), bound - _position);
        content.AsMemory(_position, count).CopyTo(buffer);
        _position += count;
        return count;
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
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
