using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/transport.test.ts</c>, plus retry, idempotency and decoding cases.
/// </summary>
public class TransportTests
{
    private static MultipartFormDataContent Form()
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("data"u8.ToArray()), "file", "f.bin");
        return form;
    }

    [Test]
    public async Task SendsBearerUserAgentAndJsonContentTypeOnJsonPost()
    {
        var handler = FakeHandler.Json("""{"ok":true}""");
        var transport = TestClient.Transport(handler);

        var body = await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/api/v1/health",
            new AgentMemoryRequestOptions { Body = new JsonObject { ["a"] = 1 } }
        );

        JsonAssert.Equivalent(body!.Value, """{"ok":true}""");
        var request = handler.Last;
        request.Header("Authorization").Should().Be("Bearer k");
        request.Header("User-Agent").Should().StartWith("surrealdb-memory-dotnet/");
        request.Header("Content-Type").Should().Be("application/json");
        request.Header("Accept").Should().Be("application/json");
        JsonAssert.Equivalent(request.Json, """{"a":1}""");
    }

    [Test]
    public async Task RetriesGetOn500ThenSucceeds()
    {
        var calls = 0;
        var handler = new FakeHandler(_ =>
        {
            calls++;
            return calls == 1
                ? Responses.Json("""{"title":"a"}""", HttpStatusCode.InternalServerError)
                : Responses.Json("""{"hits":[]}""");
        });
        var transport = TestClient.Transport(handler, maxRetries: 3);

        var body = await transport.RequestJsonAsync(HttpMethod.Get, "/x");

        JsonAssert.Equivalent(body!.Value, """{"hits":[]}""");
        calls.Should().Be(2);
    }

    [Test]
    public async Task DoesNotRetryPostOn500()
    {
        var handler = FakeHandler.Json("""{"title":"err"}""", HttpStatusCode.InternalServerError);
        var transport = TestClient.Transport(handler, maxRetries: 3);

        var act = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "/x",
                new AgentMemoryRequestOptions { Body = new JsonObject() }
            );

        await act.Should().ThrowAsync<AgentMemoryServerException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task TimesOutAsAConnectionError()
    {
        var transport = TestClient.Transport(
            FakeHandler.Stalled(),
            timeout: TimeSpan.FromMilliseconds(20),
            maxRetries: 0
        );

        var act = () => transport.RequestJsonAsync(HttpMethod.Get, "/y");

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Status.Should().Be(0);
        error.Which.Title.Should().Be("Request timed out");
        error.Which.Detail.Should().Be("Exceeded 20ms");
    }

    [Test]
    public async Task MultipartRequestsAreNotAbortedByTheDefaultTimeout()
    {
        var transport = TestClient.Transport(
            FakeHandler.Slow(TimeSpan.FromMilliseconds(150)),
            timeout: TimeSpan.FromMilliseconds(20),
            maxRetries: 0
        );

        var body = await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/documents",
            new AgentMemoryRequestOptions { Content = Form() }
        );

        JsonAssert.Equivalent(body!.Value, """{"ok":true}""");
    }

    [Test]
    public async Task MultipartRequestsStillHonorAnExplicitPerRequestTimeout()
    {
        var transport = TestClient.Transport(
            FakeHandler.Slow(TimeSpan.FromSeconds(5)),
            maxRetries: 0
        );

        var act = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "/documents",
                new AgentMemoryRequestOptions
                {
                    Content = Form(),
                    Timeout = TimeSpan.FromMilliseconds(20),
                }
            );

        await act.Should().ThrowAsync<AgentMemoryConnectionException>();
    }

    [Test]
    public async Task JsonRequestsAreStillAbortedByTheDefaultTimeout()
    {
        var transport = TestClient.Transport(
            FakeHandler.Slow(TimeSpan.FromSeconds(5)),
            timeout: TimeSpan.FromMilliseconds(20),
            maxRetries: 0
        );

        var act = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "/x",
                new AgentMemoryRequestOptions { Body = new JsonObject { ["a"] = 1 } }
            );

        await act.Should().ThrowAsync<AgentMemoryConnectionException>();
    }

    [Test]
    public async Task ACallersTokenCancelsAMultipartSend()
    {
        var transport = TestClient.Transport(FakeHandler.Stalled(), maxRetries: 0);
        using var cancellation = new CancellationTokenSource();

        var pending = transport.RequestJsonAsync(
            HttpMethod.Post,
            "/documents",
            new AgentMemoryRequestOptions { Content = Form() },
            cancellation.Token
        );
        await cancellation.CancelAsync();

        var act = () => pending;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task CancellationIsReportedApartFromATimeout()
    {
        // Both end the request through a token; only the timeout describes something that went wrong.
        var transport = TestClient.Transport(
            FakeHandler.Slow(TimeSpan.FromSeconds(5)),
            timeout: TimeSpan.FromMilliseconds(20),
            maxRetries: 0
        );

        var act = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "/x",
                new AgentMemoryRequestOptions { Body = new JsonObject { ["a"] = 1 } }
            );

        var error = await act.Should().ThrowAsync<Exception>();
        error.Which.Should().BeOfType<AgentMemoryConnectionException>();
        error.Which.Should().NotBeAssignableTo<OperationCanceledException>();
    }

    [Test]
    public async Task AnAlreadyCancelledTokenCancelsBeforeTheRequestIsSent()
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler, maxRetries: 0);

        var act = () =>
            transport.RequestJsonAsync(
                HttpMethod.Get,
                "/x",
                cancellationToken: new CancellationToken(canceled: true)
            );

        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task ACancelledRequestIsNotRetried()
    {
        // A connection failure is retried, so without the cancellation branch an aborted GET would be re-sent.
        var handler = FakeHandler.Stalled();
        var transport = TestClient.Transport(handler, maxRetries: 3);
        using var cancellation = new CancellationTokenSource();

        var pending = transport.RequestJsonAsync(
            HttpMethod.Get,
            "/x",
            cancellationToken: cancellation.Token
        );
        await Task.Delay(20);
        await cancellation.CancelAsync();

        var act = () => pending;
        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task AMultipartSendWithoutAProgressListenerIsSentAsIs()
    {
        var handler = FakeHandler.Json("""{"id":"d1"}""");
        var transport = TestClient.Transport(handler);

        await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/documents",
            new AgentMemoryRequestOptions { Content = Form() }
        );

        handler.Requests.Should().HaveCount(1);
        handler.Last.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Test]
    public async Task AMultipartSendWithAProgressListenerReportsTheBytesSent()
    {
        var handler = FakeHandler.Json("""{"id":"d1"}""");
        var transport = TestClient.Transport(handler);
        var progress = new RecordingProgress<UploadProgress>();
        var form = Form();
        var total = form.Headers.ContentLength;

        await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/documents",
            new AgentMemoryRequestOptions { Content = form, UploadProgress = progress }
        );

        progress.Reports.Should().NotBeEmpty();
        progress.Reports[^1].Loaded.Should().Be(total);
        progress.Reports.Should().OnlyContain(report => report.Total == total);
        progress.Reports.Select(report => report.Loaded).Should().BeInAscendingOrder();
        handler.Last.Header("Content-Type").Should().StartWith("multipart/form-data");
    }

    [Test]
    public async Task RetriesAnIdempotentPostWithTheSameIdempotencyKey()
    {
        var calls = 0;
        var handler = new FakeHandler(_ =>
            ++calls == 1
                ? Responses.Json("""{"e":1}""", HttpStatusCode.ServiceUnavailable)
                : Responses.Json("""{"ok":true}""")
        );
        var transport = TestClient.Transport(handler);

        await transport.RequestJsonAsync(
            HttpMethod.Post,
            "/api/v1/x/facts",
            new AgentMemoryRequestOptions
            {
                Body = new JsonObject { ["text"] = "hi" },
                Idempotent = true,
            }
        );

        handler.Requests.Should().HaveCount(2);
        var keys = handler.Requests.Select(request => request.Header("Idempotency-Key")).ToList();
        keys[0].Should().NotBeNullOrEmpty();
        keys[1].Should().Be(keys[0]);
        handler.Requests.Select(request => request.BodyText).Distinct().Should().HaveCount(1);
    }

    [Test]
    public async Task RetriesAGetAfterAConnectionFailure()
    {
        var calls = 0;
        var handler = new FakeHandler(_ =>
            ++calls == 1
                ? throw new HttpRequestException("connection refused")
                : Responses.Json("""{"ok":true}""")
        );
        var transport = TestClient.Transport(handler);

        var body = await transport.RequestJsonAsync(HttpMethod.Get, "/x");

        JsonAssert.Equivalent(body!.Value, """{"ok":true}""");
        calls.Should().Be(2);
    }

    [Test]
    public async Task ReportsAConnectionFailureOnANonIdempotentWrite()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("connection refused"));
        var transport = TestClient.Transport(handler);

        var act = () =>
            transport.RequestJsonAsync(
                HttpMethod.Post,
                "/x",
                new AgentMemoryRequestOptions { Body = new JsonObject() }
            );

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Title.Should().Be("Connection failed");
        error.Which.Detail.Should().Be("connection refused");
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task GivesUpAfterMaxRetries()
    {
        var handler = FakeHandler.Json("""{"title":"down"}""", HttpStatusCode.BadGateway);
        var transport = TestClient.Transport(handler, maxRetries: 2);

        var act = () => transport.RequestJsonAsync(HttpMethod.Get, "/x");

        var error = await act.Should().ThrowAsync<AgentMemoryServerException>();
        error.Which.Status.Should().Be(502);
        error.Which.Title.Should().Be("down");
        handler.Requests.Should().HaveCount(3);
    }

    [Test]
    public async Task ZeroMaxRetriesDisablesRetries()
    {
        var handler = FakeHandler.Json("{}", HttpStatusCode.ServiceUnavailable);
        var transport = TestClient.Transport(handler, maxRetries: 0);

        var act = () => transport.RequestJsonAsync(HttpMethod.Get, "/x");

        await act.Should().ThrowAsync<AgentMemoryServerException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task DoesNotRetryAClientError()
    {
        var handler = FakeHandler.Json("""{"message":"gone"}""", HttpStatusCode.NotFound);
        var transport = TestClient.Transport(handler);

        var act = () => transport.RequestJsonAsync(HttpMethod.Get, "/missing");

        var error = await act.Should().ThrowAsync<AgentMemoryNotFoundException>();
        error.Which.Title.Should().Be("gone");
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task ReturnsNullForAnEmptyBody()
    {
        var transport = TestClient.Transport(new FakeHandler(_ => Responses.Empty()));

        var body = await transport.RequestJsonAsync(HttpMethod.Delete, "/x");

        body.Should().BeNull();
    }

    [Test]
    public async Task ReturnsANonJsonBodyAsAJsonString()
    {
        var transport = TestClient.Transport(new FakeHandler(_ => Responses.Text("pong")));

        var body = await transport.RequestJsonAsync(HttpMethod.Get, "/x");

        body!.Value.ValueKind.Should().Be(JsonValueKind.String);
        body.Value.GetString().Should().Be("pong");
    }

    [Test]
    public async Task EncodesQueryParametersAndSkipsNullValues()
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);

        await transport.RequestJsonAsync(
            HttpMethod.Get,
            "/x",
            new AgentMemoryRequestOptions
            {
                Query =
                [
                    new("entity", "person/tobie"),
                    new("skipped", null),
                    new("since", "2026-01-01T00:00:00Z"),
                    new("q", "a b&c"),
                ],
            }
        );

        handler
            .Last.UrlText.Should()
            .Be(
                "https://example.test/x?entity=person%2Ftobie&since=2026-01-01T00%3A00%3A00Z&q=a%20b%26c"
            );
    }

    [Test]
    public async Task KeepsAnEndpointPathPrefixAndDropsATrailingSlash()
    {
        var handler = FakeHandler.Json("{}");
        var transport = new AgentMemoryTransport(
            new HttpClient(handler),
            "https://example.test/memory/",
            "k"
        );

        await transport.RequestJsonAsync(HttpMethod.Get, "/api/v1/health");

        handler.Last.UrlText.Should().Be("https://example.test/memory/api/v1/health");
    }

    [Test]
    public async Task WithOnBehalfOfAddsTheDelegationHeaderAndLeavesTheOriginalUnchanged()
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);

        await transport.WithOnBehalfOf("principal:alex").RequestJsonAsync(HttpMethod.Get, "/x");
        await transport.RequestJsonAsync(HttpMethod.Get, "/x");

        handler.Requests[0].Header("X-Spectron-On-Behalf-Of").Should().Be("principal:alex");
        handler.Requests[1].Header("X-Spectron-On-Behalf-Of").Should().BeNull();
    }

    [Test]
    public async Task RequestBytesReturnsTheBodyAndRetriesAServerError()
    {
        var calls = 0;
        var handler = new FakeHandler(_ =>
            ++calls == 1
                ? Responses.Json("{}", HttpStatusCode.InternalServerError)
                : Responses.Bytes([1, 2, 3])
        );
        var transport = TestClient.Transport(handler);

        var bytes = await transport.RequestBytesAsync("/raw");

        bytes.Should().Equal(1, 2, 3);
        handler.Last.Header("Accept").Should().Be("*/*");
        calls.Should().Be(2);
    }

    [Test]
    public async Task RequestBytesMapsAnErrorResponse()
    {
        var transport = TestClient.Transport(
            FakeHandler.Json("""{"title":"Missing"}""", HttpStatusCode.NotFound)
        );

        var act = () => transport.RequestBytesAsync("/raw");

        await act.Should().ThrowAsync<AgentMemoryNotFoundException>();
    }

    [Test]
    public async Task ABodyThatDoesNotMatchTheModelIsReportedAsAnUnexpectedResponse()
    {
        // The generated models validate required fields while reading.
        var client = TestClient.Create(FakeHandler.Json("""{"unexpected":true}"""));

        var act = () => client.WhoamiAsync();

        var error = await act.Should().ThrowExactlyAsync<AgentMemoryException>();
        error.Which.Title.Should().Be("Unexpected response body");
    }

    [Test]
    public async Task StreamMapsAnErrorResponseBeforeTheStreamOpens()
    {
        var handler = new FakeHandler(_ =>
        {
            var response = Responses.Json(
                """{"title":"Slow down"}""",
                HttpStatusCode.TooManyRequests
            );
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3));
            return response;
        });
        var transport = TestClient.Transport(handler);

        var act = () => transport.StreamAsync(HttpMethod.Post, "/chat", new JsonObject());

        var error = await act.Should().ThrowAsync<AgentMemoryRateLimitException>();
        error.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(3));
        handler.Last.Header("Accept").Should().Be("text/event-stream");
    }

    [Test]
    public void RejectsAMissingEndpointOrApiKey()
    {
        var http = new HttpClient();

        var noEndpoint = () => new AgentMemoryTransport(http, "", "k");
        var noKey = () => new AgentMemoryTransport(http, "https://example.test", " ");
        var relative = () => new AgentMemoryTransport(http, "/relative", "k");

        noEndpoint.Should().Throw<ArgumentException>();
        noKey.Should().Throw<ArgumentException>();
        relative.Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task RejectsAnAbsoluteUrlSoTheKeyOnlyGoesToTheEndpoint()
    {
        var handler = FakeHandler.Json("{}");
        var transport = TestClient.Transport(handler);

        var json = () =>
            transport.RequestJsonAsync(HttpMethod.Get, "https://attacker.example/steal");
        var bytes = () => transport.RequestBytesAsync("http://attacker.example/steal");
        var stream = () => transport.StreamAsync(HttpMethod.Post, "https://attacker.example/steal");

        await json.Should().ThrowAsync<ArgumentException>();
        await bytes.Should().ThrowAsync<ArgumentException>();
        await stream.Should().ThrowAsync<ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }
}
