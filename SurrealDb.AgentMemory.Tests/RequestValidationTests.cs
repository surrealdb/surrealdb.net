using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// What the transport refuses before anything is sent, and what it does with a request it refuses.
/// </summary>
public class RequestValidationTests
{
    private static AgentMemoryOptions Options(string endpoint = "https://api.test")
    {
        return new AgentMemoryOptions
        {
            Context = "c",
            ApiKey = "k",
            Endpoint = endpoint,
        };
    }

    [Test]
    [Arguments("https://api.test\n")]
    [Arguments("https://api.test\r\n")]
    [Arguments("https://api.test ")]
    [Arguments(" https://api.test")]
    [Arguments("https://api.test\t")]
    [Arguments("https://api .test")]
    public void RejectsAnEndpointWithWhitespaceInIt(string endpoint)
    {
        // Uri.TryCreate trims, so a value that kept its trailing newline used to pass and then fail
        // every request with a UriFormatException.
        var act = () => new AgentMemoryClient(Options(endpoint), new HttpClient());

        act.Should().Throw<ArgumentException>().WithMessage("*trailing newline*");
    }

    [Test]
    [Arguments("https://api.test?tenant=1")]
    [Arguments("https://api.test/#section")]
    [Arguments("https://api.test/base?x=1#y")]
    public void RejectsAnEndpointWithAQueryOrAFragment(string endpoint)
    {
        // Paths are appended to the endpoint: a query or a fragment would swallow them, and every
        // call would reach the endpoint's root.
        var act = () => new AgentMemoryClient(Options(endpoint), new HttpClient());

        act.Should().Throw<ArgumentException>().WithMessage("*query string or a fragment*");
    }

    [Test]
    [Arguments("https://api.test")]
    [Arguments("https://api.test/")]
    [Arguments("https://api.test/base/")]
    [Arguments("HTTPS://API.TEST")]
    [Arguments("http://localhost:8000")]
    public void AcceptsOrdinaryEndpoints(string endpoint)
    {
        var client = new AgentMemoryClient(Options(endpoint), new HttpClient());

        client.ContextId.Should().Be("c");
    }

    [Test]
    public async Task RefusesAPerRequestTimeoutOfZeroOrLess()
    {
        // A spent time budget must not turn into no deadline at all.
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);
        var client = TestClient.Create(handler);

        var download = () => transport.RequestBytesAsync("/raw", timeout: TimeSpan.Zero);
        var request = () =>
            transport.RequestJsonAsync(
                HttpMethod.Get,
                "/x",
                new AgentMemoryRequestOptions { Timeout = TimeSpan.FromMilliseconds(-5) }
            );
        var raw = () => client.Documents.RawAsync("d1", TimeSpan.Zero);
        var upload = () =>
            client.Documents.UploadAsync(
                new DocumentUploadOptions { File = new byte[] { 1 }, Timeout = TimeSpan.Zero }
            );

        await download.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await request.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await raw.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await upload.Should().ThrowAsync<ArgumentOutOfRangeException>();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task AnInfinitePerRequestTimeoutLiftsTheDefaultDeadline()
    {
        var transport = TestClient.Transport(
            FakeHandler.Slow(TimeSpan.FromMilliseconds(150)),
            timeout: TimeSpan.FromMilliseconds(20),
            maxRetries: 0
        );

        var result = await transport.RequestJsonAsync(
            HttpMethod.Get,
            "/x",
            new AgentMemoryRequestOptions { Timeout = Timeout.InfiniteTimeSpan }
        );

        result.Should().NotBeNull();
    }

    [Test]
    [Arguments("ftp://elsewhere.example/x")]
    [Arguments("HTTPS://elsewhere.example/x")]
    [Arguments("custom+scheme-1.0://elsewhere.example/x")]
    public async Task RejectsAPathThatStartsWithAnyScheme(string path)
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);

        var act = () => transport.RequestJsonAsync(HttpMethod.Get, path);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*absolute URL*");
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task AUrlInsideAQueryIsNotMistakenForAnAbsolutePath()
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);

        await transport.RequestJsonAsync(
            HttpMethod.Get,
            "/api/v1/c/inspect?ref=https://example.com/a"
        );

        handler
            .Last.UrlText.Should()
            .Be("https://example.test/api/v1/c/inspect?ref=https://example.com/a");
    }

    [Test]
    [Arguments("//elsewhere.example/x")]
    [Arguments("@elsewhere.example/x")]
    [Arguments("\\\\elsewhere.example\\x")]
    public async Task APathThatOnlyLooksLikeAnotherHostStaysOnTheEndpoint(string path)
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);

        await transport.RequestJsonAsync(HttpMethod.Get, path);

        handler.Last.Url.Host.Should().Be("example.test");
        handler.Last.Header("Authorization").Should().Be("Bearer k");
    }

    [Test]
    public async Task DisposesTheContentOfARequestItRefuses()
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);
        var refusedPath = new TrackedContent();
        var refusedTimeout = new TrackedContent();

        var path = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "https://elsewhere.example/x",
                new AgentMemoryRequestOptions { Content = refusedPath }
            );
        var timeout = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "/x",
                new AgentMemoryRequestOptions { Content = refusedTimeout, Timeout = TimeSpan.Zero }
            );

        await path.Should().ThrowAsync<ArgumentException>();
        await timeout.Should().ThrowAsync<ArgumentOutOfRangeException>();
        refusedPath.Disposed.Should().BeTrue();
        refusedTimeout.Disposed.Should().BeTrue();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task AsksTheServiceToAnswerBeforeAnUploadIsSentInFull()
    {
        // The service can refuse an upload from its headers alone and then closes the connection.
        // With Expect: 100-continue the refusal arrives as its status; without it, a large body dies
        // on a failed write and the caller sees a connection error.
        var handler = FakeHandler.Json("""{"id":"d1"}""");
        var transport = TestClient.Transport(handler);
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("data"u8.ToArray()), "file", "f.bin");

        await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/upload",
            new AgentMemoryRequestOptions { Content = form }
        );
        await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/json",
            new AgentMemoryRequestOptions { Body = new JsonObject { ["a"] = 1 } }
        );

        handler.Requests[0].Header("Expect").Should().Be("100-continue");
        handler.Requests[1].Header("Expect").Should().BeNull();
    }

    [Test]
    public async Task DownloadsABodyThatDeclaresNoLength()
    {
        var content = Enumerable.Range(0, 200_000).Select(i => (byte)i).ToArray();
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new FlakyStream(content)),
        });

        var bytes = await TestClient.Transport(handler).RequestBytesAsync("/raw");

        bytes.Should().Equal(content);
    }

    [Test]
    public async Task RefusesADownloadTooLargeForAByteArrayWithoutFetchingItAgain()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new OversizedContent(),
        });
        var transport = TestClient.Transport(handler, maxRetries: 3);

        var act = () => transport.RequestBytesAsync("/raw");

        var error = await act.Should().ThrowExactlyAsync<AgentMemoryException>();
        error.Which.Title.Should().Be("Response too large");
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task RetriesADownloadWhoseConnectionDropsPartWay()
    {
        var time = new InstantTimeProvider();
        var attempts = 0;
        var handler = new FakeHandler(_ =>
            ++attempts == 1
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(
                        new FlakyStream(new byte[4096], failAfter: 1024, failEveryTime: true)
                    ),
                }
                : Responses.Bytes([1, 2, 3])
        );
        var transport = new AgentMemoryTransport(
            new HttpClient(handler),
            "https://example.test",
            "k",
            Timeout.InfiniteTimeSpan,
            3,
            null,
            time
        );

        var bytes = await transport.RequestBytesAsync("/raw");

        bytes.Should().Equal(1, 2, 3);
        handler.Requests.Should().HaveCount(2);
    }

    [Test]
    public async Task ReportsADownloadThatKeepsDroppingAsAConnectionError()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(
                new FlakyStream(new byte[4096], failAfter: 1024, failEveryTime: true)
            ),
        });
        var transport = TestClient.Transport(handler, maxRetries: 0);

        var act = () => transport.RequestBytesAsync("/raw");

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Title.Should().Be("Connection failed");
    }

    private sealed class TrackedContent() : ByteArrayContent([1, 2, 3])
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Declares three gibibytes and never produces them.
    /// </summary>
    private sealed class OversizedContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            throw new InvalidOperationException("The body must not be read.");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 3L * 1024 * 1024 * 1024;
            return true;
        }
    }

    [Test]
    public async Task AnUploadThatDiesWhileItIsSentSaysTheServiceMayHaveRefusedIt()
    {
        // Once the body is flowing, the service refuses an upload by answering and closing the
        // connection. The write that fails hides the answer, so the error says what may have happened.
        var handler = new FakeHandler(
            (_, _) =>
                throw new HttpRequestException(
                    "Error while copying content to a stream.",
                    new IOException("Broken pipe")
                )
        );

        var act = () =>
            TestClient
                .Create(handler)
                .Documents.UploadAsync(
                    new DocumentUploadOptions { File = new byte[] { 1 }, FileName = "a.pdf" }
                );

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Detail.Should().StartWith("Error while copying content to a stream.");
        error.Which.Detail.Should().Contain("may have refused the upload");
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task AnUploadThatNeverConnectedIsAPlainConnectionError()
    {
        var handler = new FakeHandler(
            (_, _) =>
                throw new HttpRequestException(
                    HttpRequestError.NameResolutionError,
                    "No such host is known."
                )
        );

        var act = () =>
            TestClient
                .Create(handler)
                .Documents.UploadAsync(
                    new DocumentUploadOptions { File = new byte[] { 1 }, FileName = "a.pdf" }
                );

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Detail.Should().Be("No such host is known.");
    }

    [Test]
    public async Task ADownloadWhoseReadFailsBecauseItWasCancelledIsACancellation()
    {
        // Aborting a request makes the read under it fail with an I/O error. That is the caller's
        // cancellation, so it is neither retried nor reported as a dropped connection.
        using var cancellation = new CancellationTokenSource();
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new AbortedStream(cancellation)),
        });
        var transport = TestClient.Transport(handler, maxRetries: 3);

        var act = () => transport.RequestBytesAsync("/raw", cancellationToken: cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task ADownloadWhoseReadFailsAtTheDeadlineIsATimeout()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new AbortedStream(null)),
        });
        var transport = TestClient.Transport(handler, maxRetries: 3);

        var act = () => transport.RequestBytesAsync("/raw", timeout: TimeSpan.FromMilliseconds(20));

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Title.Should().Be("Request timed out");
        handler.Requests.Should().HaveCount(1);
    }

    /// <summary>
    /// A body that never arrives. When the read is cancelled it fails with an I/O error, as a socket
    /// does when the request is aborted under it; given a source, it cancels that first.
    /// </summary>
    private sealed class AbortedStream(CancellationTokenSource? cancelFirst) : Stream
    {
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
            if (cancelFirst is not null)
            {
                await cancelFirst.CancelAsync();
            }

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new IOException("The request was aborted.");
            }

            return 0;
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
            throw new NotSupportedException();
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
}
