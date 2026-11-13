using System.Net;
using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// How long a retry waits, and which responses are worth one. The waits are read from the clock
/// rather than sat through.
/// </summary>
public class RetryPolicyTests
{
    private static AgentMemoryTransport Transport(FakeHandler handler, InstantTimeProvider time)
    {
        return new AgentMemoryTransport(
            new HttpClient(handler),
            "https://example.test",
            "k",
            Timeout.InfiniteTimeSpan,
            3,
            null,
            time
        );
    }

    private static HttpResponseMessage Busy(string retryAfter)
    {
        var response = Responses.Json(
            """{"message":"server at capacity; retry shortly"}""",
            HttpStatusCode.ServiceUnavailable
        );
        response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        return response;
    }

    [Test]
    public async Task WaitsTheBackOffScheduleBetweenRetries()
    {
        var time = new InstantTimeProvider();
        var handler = FakeHandler.Json("""{"title":"err"}""", HttpStatusCode.InternalServerError);

        var act = () => Transport(handler, time).RequestJsonAsync(HttpMethod.Get, "/x");

        await act.Should().ThrowAsync<AgentMemoryServerException>();
        handler.Requests.Should().HaveCount(4);
        time.Delays.Select(delay => (int)delay.TotalMilliseconds).Should().Equal(250, 500, 1000);
    }

    [Test]
    public async Task WaitsAsLongAsTheServiceAsksWhenThatIsLongerThanTheBackOff()
    {
        // The service sends Retry-After: 1 with every 503 while it sheds load. Coming back after 250 ms
        // would land while it is still saturated.
        var time = new InstantTimeProvider();
        var attempts = 0;
        var handler = new FakeHandler(_ =>
            ++attempts switch
            {
                1 => Busy("1"),
                2 => Responses.Json("""{"message":"boom"}""", HttpStatusCode.InternalServerError),
                _ => Responses.Json("""{"ok":true}"""),
            }
        );

        await Transport(handler, time).RequestJsonAsync(HttpMethod.Get, "/x");

        time.Delays.Should().Equal(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500));
    }

    [Test]
    public async Task KeepsTheBackOffWhenTheServiceAsksForLess()
    {
        var time = new InstantTimeProvider();
        var attempts = 0;
        var handler = new FakeHandler(_ =>
            ++attempts == 1 ? Busy("0") : Responses.Json("""{"ok":true}""")
        );

        await Transport(handler, time).RequestJsonAsync(HttpMethod.Get, "/x");

        time.Delays.Should().Equal(TimeSpan.FromMilliseconds(250));
    }

    [Test]
    public async Task CutsARetryAfterThatWouldStallTheCaller()
    {
        var time = new InstantTimeProvider();
        var attempts = 0;
        var handler = new FakeHandler(_ =>
            ++attempts == 1 ? Busy("86400") : Responses.Json("""{"ok":true}""")
        );

        await Transport(handler, time).RequestJsonAsync(HttpMethod.Get, "/x");

        time.Delays.Should().Equal(AgentMemoryRetry.MaxRetryAfter);
    }

    [Test]
    public async Task HonoursRetryAfterOnADownloadToo()
    {
        var time = new InstantTimeProvider();
        var attempts = 0;
        var handler = new FakeHandler(_ =>
            ++attempts == 1 ? Busy("1") : Responses.Bytes([1, 2, 3])
        );

        var bytes = await Transport(handler, time).RequestBytesAsync("/raw");

        bytes.Should().Equal(1, 2, 3);
        time.Delays.Should().Equal(TimeSpan.FromSeconds(1));
    }

    [Test]
    [Arguments(501)]
    [Arguments(505)]
    public async Task DoesNotRetryAStatusThatASecondTryCannotChange(int status)
    {
        // The service answers 501 to an inspect of a passage reference, every time.
        var handler = FakeHandler.Json(
            """{"message":"passage inspect is not yet supported"}""",
            (HttpStatusCode)status
        );
        var transport = TestClient.Transport(handler, maxRetries: 3);

        var read = () => transport.RequestJsonAsync(HttpMethod.Get, "/x");
        var download = () => transport.RequestBytesAsync("/raw");

        await read.Should().ThrowAsync<AgentMemoryServerException>();
        await download.Should().ThrowAsync<AgentMemoryServerException>();
        handler.Requests.Should().HaveCount(2);
    }

    [Test]
    public async Task ComesBackWhenAnEarlierAttemptWithTheSameKeyIsStillRunning()
    {
        // The service answers 409 while the first request with this idempotency key is in flight, and
        // returns that request's stored response once it has finished.
        var time = new InstantTimeProvider();
        var attempts = 0;
        var handler = new FakeHandler(_ =>
            ++attempts < 3
                ? Responses.Json(
                    """{"message":"A request with this Idempotency-Key is already in progress"}""",
                    HttpStatusCode.Conflict
                )
                : Responses.Json(Fixtures.FactsResponse)
        );
        var client = TestClient.Create(
            handler,
            options =>
            {
                options.TimeProvider = time;
                options.Timeout = Timeout.InfiniteTimeSpan;
            }
        );

        var result = await client.RememberAsync("hello");

        result.SessionId.Should().Be("s1");
        handler.Requests.Should().HaveCount(3);
        handler
            .Requests.Select(request => request.Header("Idempotency-Key"))
            .Distinct()
            .Should()
            .ContainSingle();
        time.Delays.Select(delay => (int)delay.TotalMilliseconds).Should().Equal(250, 500);
    }

    [Test]
    public async Task GivesUpOnARequestThatStaysInProgress()
    {
        var time = new InstantTimeProvider();
        var handler = FakeHandler.Json(
            """{"message":"A request with this Idempotency-Key is already in progress"}""",
            HttpStatusCode.Conflict
        );
        var client = TestClient.Create(
            handler,
            options =>
            {
                options.TimeProvider = time;
                options.Timeout = Timeout.InfiniteTimeSpan;
            }
        );

        var act = () => client.RememberAsync("hello");

        var error = await act.Should().ThrowAsync<AgentMemoryException>();
        error.Which.Status.Should().Be(409);
        handler.Requests.Should().HaveCount(4);
    }

    [Test]
    public async Task DoesNotRetryAnyOtherConflict()
    {
        // A label cap is a 409 as well, and it is the same on every try.
        var handler = FakeHandler.Json(
            """{"message":"too many labels on one fact"}""",
            HttpStatusCode.Conflict
        );

        var act = () => TestClient.Create(handler).RememberAsync("hello");

        var error = await act.Should().ThrowAsync<AgentMemoryException>();
        error.Which.Title.Should().Be("too many labels on one fact");
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task DoesNotRetryAWriteThatIsNotIdempotentWhateverTheConflictSays()
    {
        var handler = FakeHandler.Json(
            """{"message":"A request with this Idempotency-Key is already in progress"}""",
            HttpStatusCode.Conflict
        );

        var act = () => TestClient.Create(handler).ForgetAsync("old notes");

        await act.Should().ThrowAsync<AgentMemoryException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    [Arguments("GET", 501, false)]
    [Arguments("GET", 505, false)]
    [Arguments("POST", 501, true)]
    [Arguments("HEAD", 505, false)]
    public void ShouldRetryLeavesOutStatusesThatDescribeTheRequest(
        string method,
        int status,
        bool idempotent
    )
    {
        AgentMemoryRetry.ShouldRetry(method, status, 0, 3, idempotent).Should().BeFalse();
    }
}
