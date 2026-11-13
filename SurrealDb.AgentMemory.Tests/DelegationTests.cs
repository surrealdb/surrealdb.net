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
    public async Task ADelegatedClientSharesTheConnection()
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var client = TestClient.Create(handler);

        var delegated = client.OnBehalfOf("principal:alex");
        await delegated.WhoamiAsync();
        await client.WhoamiAsync();

        delegated.ContextId.Should().Be(client.ContextId);
        handler.Requests.Should().HaveCount(2);
    }

    [Test]
    [Arguments("alex\n")]
    [Arguments("alex\r\nX-Injected: yes")]
    [Arguments("alex\r\n\r\nDELETE /api/v1/c/entities/person/tobie HTTP/1.1")]
    [Arguments("\talex")]
    [Arguments("al\u0000ex")]
    [Arguments("zo\u00eb")]
    [Arguments("\u30a2\u30ec\u30c3\u30af\u30b9")]
    public async Task RefusesAPrincipalIdThatAHeaderCannotCarry(string principalId)
    {
        // The id becomes a request header. A line break in it used to start a new header, or a whole
        // new request, on the wire; a character outside ASCII failed every call as a connection error.
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var client = TestClient.Create(handler);

        var act = () => client.OnBehalfOf(principalId);

        act.Should().Throw<ArgumentException>().WithParameterName("principalId");
        await client.WhoamiAsync();
        handler.Last.Header("X-Spectron-On-Behalf-Of").Should().BeNull();
    }

    [Test]
    [Arguments(" ")]
    [Arguments("   ")]
    [Arguments("principal:")]
    [Arguments(" alex")]
    [Arguments("alex ")]
    [Arguments("principal:alex ")]
    public void RefusesAPrincipalIdTheServiceWouldReadAsNoDelegation(string principalId)
    {
        // The service trims the header, strips "principal:", and runs the request with the API key's
        // own grants when nothing is left. "principal:" + an empty user id must not get that far.
        var client = TestClient.Create(FakeHandler.Json("{}"));

        var act = () => client.OnBehalfOf(principalId);

        act.Should().Throw<ArgumentException>().WithParameterName("principalId");
    }

    [Test]
    [Arguments("principal:alex")]
    [Arguments("agent-b")]
    [Arguments("team lead")]
    [Arguments("user:42@example.com")]
    public async Task SendsAnOrdinaryPrincipalIdAsItIs(string principalId)
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);

        await TestClient.Create(handler).OnBehalfOf(principalId).WhoamiAsync();

        handler.Last.Header("X-Spectron-On-Behalf-Of").Should().Be(principalId);
    }

    [Test]
    public void TheTransportChecksThePrincipalIdItIsBuiltWith()
    {
        var withLineBreak = () =>
            new AgentMemoryTransport(
                new HttpClient(),
                "https://api.test",
                "k",
                onBehalfOf: "alex\r\nX-Injected: yes"
            );
        var blank = () =>
            new AgentMemoryTransport(new HttpClient(), "https://api.test", "k", onBehalfOf: "");
        var transport = new AgentMemoryTransport(new HttpClient(), "https://api.test", "k");

        withLineBreak.Should().Throw<ArgumentException>();
        blank.Should().Throw<ArgumentException>();
        FluentActions
            .Invoking(() => transport.WithOnBehalfOf("principal:"))
            .Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public async Task DelegatedWritesForDifferentPrincipalsDoNotShareAnIdempotencyKey()
    {
        // The service keeps one slot per authenticated principal and key, and under delegation that
        // principal is the API key's own. With a shared key, the second user's identical write would be
        // answered with the first user's response and never stored.
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1767225610000);
        var handler = FakeHandler.Json(Fixtures.FactsResponse);
        var client = TestClient.Create(
            handler,
            options => options.TimeProvider = new FixedTimeProvider(now)
        );

        await client.OnBehalfOf("user-a").RememberAsync("hello");
        await client.OnBehalfOf("user-b").RememberAsync("hello");
        await client.OnBehalfOf("user-a").RememberAsync("hello");
        await client.RememberAsync("hello");

        var keys = handler.Requests.Select(request => request.Header("Idempotency-Key")).ToList();
        keys[0].Should().NotBe(keys[1]);
        keys[0].Should().Be(keys[2]);
        keys[3].Should().NotBe(keys[0]).And.NotBe(keys[1]);

        // Without delegation the key is still the one the JavaScript and Python clients derive.
        keys[3]
            .Should()
            .Be(
                AgentMemoryIdempotency.ComputeKey(
                    "POST",
                    "/api/v1/ctx-1/facts",
                    """{"text":"hello"}""",
                    now
                )
            );
        handler
            .Requests.Should()
            .OnlyContain(request => request.BodyText == """{"text":"hello"}""");
    }
}
