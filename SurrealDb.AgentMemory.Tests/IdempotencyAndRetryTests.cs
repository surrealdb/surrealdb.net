using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// The idempotency-key derivation and the retry rules. The JavaScript client exports both
/// (<c>idempotencyKey</c>, <c>shouldRetry</c>, <c>backoffSchedule</c>); its suite covers them only through the transport.
/// </summary>
public class IdempotencyAndRetryTests
{
    // 2026-01-01T00:00:10Z; the expected keys below were computed with the JavaScript client's derivation.
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(
        1767225610000
    );

    [Test]
    public void MatchesTheJavaScriptClientsDerivation()
    {
        var key = AgentMemoryIdempotency.ComputeKey(
            "POST",
            "/api/v1/ctx-1/facts",
            """{"text":"I was promoted to CTO","session_id":"s1"}""",
            Now
        );

        key.Should().Be("1f119d777826673064de6cd3680e042371a1a1e772e1f0d707800be0ca99e1aa");
    }

    [Test]
    public void IsStableWithinA30SecondBucketAndChangesAcrossBuckets()
    {
        string Key(DateTimeOffset at)
        {
            return AgentMemoryIdempotency.ComputeKey("POST", "/p", "{}", at);
        }

        var bucketStart = DateTimeOffset.FromUnixTimeSeconds(1767225600);

        Key(bucketStart).Should().Be(Key(bucketStart.AddSeconds(29.999)));
        Key(bucketStart).Should().NotBe(Key(bucketStart.AddSeconds(30)));
    }

    [Test]
    public void DependsOnTheMethodPathAndBody()
    {
        var key = AgentMemoryIdempotency.ComputeKey("POST", "/p", "{}", Now);

        AgentMemoryIdempotency.ComputeKey("PUT", "/p", "{}", Now).Should().NotBe(key);
        AgentMemoryIdempotency.ComputeKey("POST", "/q", "{}", Now).Should().NotBe(key);
        AgentMemoryIdempotency.ComputeKey("POST", "/p", "{\"a\":1}", Now).Should().NotBe(key);
        key.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Test]
    public async Task SendsTheBodyAndKeyTheJavaScriptClientWouldSend()
    {
        // The body is serialized like JSON.stringify (non-ASCII and HTML characters unescaped),
        // so the key a retry carries is the one the reference clients derive for the same write.
        var handler = FakeHandler.Json(Fixtures.FactsResponse);
        var client = TestClient.Create(
            handler,
            options => options.TimeProvider = new FixedTimeProvider(Now)
        );

        await client.RememberAsync("Café – ünïcode <b>&");

        handler.Last.BodyText.Should().Be("""{"text":"Café – ünïcode <b>&"}""");
        handler
            .Last.Header("Idempotency-Key")
            .Should()
            .Be("c01d2aa2acf1240b0ad80949873a57c255d1fa43fe94278e185802e352d7cf31");
    }

    [Test]
    [Arguments(0, new int[0])]
    [Arguments(1, new[] { 250 })]
    [Arguments(2, new[] { 250, 500 })]
    [Arguments(3, new[] { 250, 500, 1000 })]
    [Arguments(5, new[] { 250, 500, 1000 })]
    [Arguments(-1, new int[0])]
    public void BackoffScheduleIsCappedAtThreeSteps(int maxRetries, int[] expected)
    {
        AgentMemoryRetry
            .BackoffSchedule(maxRetries)
            .Select(delay => (int)delay.TotalMilliseconds)
            .Should()
            .Equal(expected);
    }

    [Test]
    [Arguments("GET", 500, 0, 3, false, true)]
    [Arguments("get", 503, 2, 3, false, true)]
    [Arguments("HEAD", 502, 0, 3, false, true)]
    [Arguments("GET", null, 0, 3, false, true)]
    [Arguments("GET", 404, 0, 3, false, false)]
    [Arguments("GET", 429, 0, 3, false, false)]
    [Arguments("GET", 500, 3, 3, false, false)]
    [Arguments("POST", 500, 0, 3, false, false)]
    [Arguments("POST", null, 0, 3, false, false)]
    [Arguments("POST", 500, 0, 3, true, true)]
    [Arguments("POST", null, 0, 3, true, true)]
    [Arguments("DELETE", 500, 0, 3, false, false)]
    public void ShouldRetryReadsAndIdempotentWritesOnServerAndConnectionFailures(
        string method,
        int? status,
        int attempt,
        int maxRetries,
        bool idempotent,
        bool expected
    )
    {
        AgentMemoryRetry
            .ShouldRetry(method, status, attempt, maxRetries, idempotent)
            .Should()
            .Be(expected);
    }
}
