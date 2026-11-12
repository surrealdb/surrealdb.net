using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/delegation.test.ts</c>.
/// </summary>
public class DelegationTests
{
    [Test]
    public async Task AddsTheOnBehalfOfHeaderOnDelegatedRequests()
    {
        var handler = FakeHandler.Json(Fixtures.QueryMemoryResponse);
        var delegated = TestClient.Create(handler).OnBehalfOf("principal:alex");

        await delegated.RecallAsync("hi");

        handler.Last.Header("X-Spectron-On-Behalf-Of").Should().Be("principal:alex");
        delegated.OnBehalfOfPrincipal.Should().Be("principal:alex");
    }

    [Test]
    public async Task PropagatesTheHeaderThroughSubNamespaces()
    {
        var handler = FakeHandler.Json($$"""{"keys":[],"page":{{Fixtures.Page()}}}""");
        var delegated = TestClient.Create(handler).OnBehalfOf("principal:alex");

        await delegated.Keys.ListAsync();

        handler.Last.Header("X-Spectron-On-Behalf-Of").Should().Be("principal:alex");
    }

    [Test]
    public async Task PropagatesTheHeaderToSessionsItCreates()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Method == "POST"
                    ? Fixtures.SessionResponse
                    : $$"""{"turns":[],"page":{{Fixtures.Page()}}}"""
            )
        );
        var delegated = TestClient.Create(handler).OnBehalfOf("principal:alex");

        var session = await delegated.Sessions.CreateAsync();
        await session.TurnsAsync();

        handler
            .Requests.Should()
            .OnlyContain(request => request.Header("X-Spectron-On-Behalf-Of") == "principal:alex");
    }

    [Test]
    public async Task LeavesTheOriginalClientUnchanged()
    {
        var handler = FakeHandler.Json(Fixtures.QueryMemoryResponse);
        var client = TestClient.Create(handler);

        client.OnBehalfOf("principal:alex");
        await client.RecallAsync("hi");

        handler.Last.Header("X-Spectron-On-Behalf-Of").Should().BeNull();
        client.OnBehalfOfPrincipal.Should().BeNull();
    }

    [Test]
    public void RejectsAnEmptyPrincipalId()
    {
        var client = TestClient.Create(FakeHandler.Json("{}"));

        FluentActions.Invoking(() => client.OnBehalfOf("")).Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task ADelegatedClientSharesTheConnectionAndDisposingItKeepsItOpen()
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var client = TestClient.Create(handler);

        using (var delegated = client.OnBehalfOf("principal:alex"))
        {
            delegated.ContextId.Should().Be(client.ContextId);
        }
        await client.WhoamiAsync();

        handler.Requests.Should().HaveCount(1);
    }
}
