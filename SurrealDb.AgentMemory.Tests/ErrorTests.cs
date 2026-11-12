using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/errors.test.ts</c>, plus the remaining status codes and body shapes.
/// </summary>
public class ErrorTests
{
    private static HttpResponseHeaders Headers(string? retryAfter = null)
    {
        var response = new HttpResponseMessage();
        if (retryAfter is not null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        }
        return response.Headers;
    }

    [Test]
    public void Maps401ToAuthException()
    {
        var error = AgentMemoryException.FromResponse(
            401,
            """{"title":"Unauthorized","detail":"bad token"}"""
        );

        error.Should().BeOfType<AgentMemoryAuthException>();
        error.Status.Should().Be(401);
        error.Detail.Should().Be("bad token");
        error.Message.Should().Be("[401] Unauthorized: bad token");
    }

    [Test]
    public void Maps404ToNotFoundException()
    {
        AgentMemoryException
            .FromResponse(404, """{"title":"Missing"}""")
            .Should()
            .BeOfType<AgentMemoryNotFoundException>();
    }

    [Test]
    public void Maps422ToValidationException()
    {
        AgentMemoryException
            .FromResponse(422, """{"title":"Invalid"}""")
            .Should()
            .BeOfType<AgentMemoryValidationException>();
    }

    [Test]
    public void Maps429ToRateLimitExceptionWithRetryAfter()
    {
        var error = AgentMemoryException.FromResponse(
            429,
            """{"title":"Slow down"}""",
            Headers(retryAfter: "12")
        );

        error.Should().BeOfType<AgentMemoryRateLimitException>();
        ((AgentMemoryRateLimitException)error).RetryAfter.Should().Be(TimeSpan.FromSeconds(12));
    }

    [Test]
    public void Maps500ToServerException()
    {
        AgentMemoryException
            .FromResponse(500, """{"title":"oops"}""")
            .Should()
            .BeOfType<AgentMemoryServerException>();
    }

    [Test]
    [Arguments(400, typeof(AgentMemoryValidationException))]
    [Arguments(403, typeof(AgentMemoryScopeException))]
    [Arguments(503, typeof(AgentMemoryServerException))]
    [Arguments(418, typeof(AgentMemoryException))]
    [Arguments(409, typeof(AgentMemoryException))]
    public void MapsEveryStatusToItsExceptionType(int status, Type expected)
    {
        AgentMemoryException.FromResponse(status, "{}").Should().BeOfType(expected);
    }

    [Test]
    public void ReadsTheProblemDetailsFieldsAndKeepsTheRestAsExtensions()
    {
        var error = AgentMemoryException.FromResponse(
            403,
            """{"type":"https://errors.test/scope","title":"Forbidden","detail":"outside the scope floor","instance":"/api/v1/c/facts","status":403,"scope":"team/eng","verbs":["write"]}"""
        );

        error.Type.Should().Be("https://errors.test/scope");
        error.Instance.Should().Be("/api/v1/c/facts");
        error.Extensions.Keys.Should().BeEquivalentTo(["scope", "verbs"]);
        error.Extensions["scope"].GetString().Should().Be("team/eng");
    }

    [Test]
    public void FallsBackToMessageWhenThereIsNoTitle()
    {
        var error = AgentMemoryException.FromResponse(404, """{"message":"gone"}""");

        error.Title.Should().Be("gone");
        error.Extensions.Should().BeEmpty();
    }

    [Test]
    public void UsesAGenericTitleAndTheTextAsDetailForAPlainTextBody()
    {
        var error = AgentMemoryException.FromResponse(502, "Bad gateway from the proxy");

        error.Title.Should().Be("Agent Memory request failed");
        error.Detail.Should().Be("Bad gateway from the proxy");
        error.Message.Should().Be("[502] Agent Memory request failed: Bad gateway from the proxy");
    }

    [Test]
    public void HandlesAnEmptyBody()
    {
        var error = AgentMemoryException.FromResponse(500, null);

        error.Title.Should().Be("Agent Memory request failed");
        error.Detail.Should().BeNull();
        error.Message.Should().Be("[500] Agent Memory request failed");
    }

    [Test]
    public void IgnoresANonNumericRetryAfter()
    {
        var error = (AgentMemoryRateLimitException)
            AgentMemoryException.FromResponse(
                429,
                "{}",
                Headers(retryAfter: "Wed, 21 Oct 2026 07:28:00 GMT")
            );

        error.RetryAfter.Should().BeNull();
    }

    [Test]
    public async Task ClientCallsSurfaceTypedErrors()
    {
        var client = Infrastructure.TestClient.Create(
            Infrastructure.FakeHandler.Json(
                """{"title":"Unauthorized","detail":"bad token"}""",
                HttpStatusCode.Unauthorized
            )
        );

        var act = () => client.RecallAsync("hi");

        var error = await act.Should().ThrowAsync<AgentMemoryAuthException>();
        error.Which.Detail.Should().Be("bad token");
    }
}
