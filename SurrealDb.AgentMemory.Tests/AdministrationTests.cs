using System.Text.Json.Nodes;
using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Sessions, lifecycle, traces, principals and scopes. The JavaScript suite covers these namespaces only through
/// <c>sessions.create</c>; these check every route and body.
/// </summary>
public class AdministrationTests
{
    [Test]
    public async Task ASessionReadsItsTurnsAndContextAndCloses()
    {
        var handler = new FakeHandler(request =>
            request.Path switch
            {
                "/api/v1/ctx-1/sessions" => Responses.Json(Fixtures.SessionResponse),
                "/api/v1/ctx-1/sessions/s1/turns" => Responses.Json(
                    request.UrlText.Contains("cursor=", StringComparison.Ordinal)
                        ? $$"""{"turns":[{{Fixtures.Turn("turn:2")}}],"page":{{Fixtures.Page()}}}"""
                        : $$"""{"turns":[{{Fixtures.Turn("turn:1")}}],"page":{{Fixtures.Page(true, "c2")}}}"""
                ),
                "/api/v1/ctx-1/sessions/s1/context" => Responses.Json("""{"context":"blob"}"""),
                _ => Responses.Empty(),
            }
        );
        var client = TestClient.Create(handler);

        var session = await client.Sessions.CreateAsync(
            new SessionCreateOptions { Metadata = new JsonObject { ["channel"] = "slack" } }
        );
        var turns = await session.AllTurnsAsync(limit: 10);
        var context = await session.ContextAsync("what happened?");
        await session.CloseAsync();

        JsonAssert.Equivalent(handler.Requests[0].Json, """{"metadata":{"channel":"slack"}}""");
        turns.Select(turn => turn.Id).Should().Equal("turn:1", "turn:2");
        handler.Requests[1].UrlText.Should().Contain("limit=10");
        context.Context.Should().Be("blob");
        JsonAssert.Equivalent(handler.Requests[3].Json, """{"query":"what happened?"}""");
        handler.Requests[3].Header("Idempotency-Key").Should().HaveLength(64);
        handler.Requests[4].Method.Should().Be("DELETE");
        handler.Requests[4].UrlText.Should().EndWith("/api/v1/ctx-1/sessions/s1");
    }

    [Test]
    public async Task LifecycleSweepsPostAnEmptyObject()
    {
        var handler = FakeHandler.Json("""{"affected":2}""");
        var client = TestClient.Create(handler);

        var expired = await client.Lifecycle.ExpireAsync();
        await client.Lifecycle.DecayAsync();

        expired.Affected.Should().Be(2);
        handler.Requests[0].UrlText.Should().EndWith("/api/v1/ctx-1/lifecycle/expire");
        handler.Requests[1].UrlText.Should().EndWith("/api/v1/ctx-1/lifecycle/decay");
        handler.Requests.Should().OnlyContain(request => request.BodyText == "{}");
    }

    [Test]
    public async Task TracesListWalkAndGet()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Path.EndsWith("/traces", StringComparison.Ordinal)
                    ? $$"""{"traces":[{{Fixtures.TraceRecord("trace:1")}}],"page":{{Fixtures.Page()}}}"""
                    : Fixtures.TraceRecord("trace:1")
            )
        );
        var client = TestClient.Create(handler);

        var page = await client.Traces.ListAsync(new PageOptions { Limit = 5 });
        var all = await client.Traces.ListAllAsync();
        var trace = await client.Traces.GetAsync("trace:1");

        page.Traces.Should().ContainSingle();
        handler.Requests[0].UrlText.Should().Contain("limit=5");
        all.Should().ContainSingle();
        trace.Id.Should().Be("trace:1");
        handler.Requests[2].UrlText.Should().EndWith("/api/v1/ctx-1/traces/trace%3A1");
    }

    [Test]
    public async Task PrincipalsReadAndChangeGrants()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Path.EndsWith("/effective", StringComparison.Ordinal)
                    ? """{"path":"team/eng","verbs":["read"]}"""
                : request.Path.EndsWith("/principals", StringComparison.Ordinal)
                    ? $$"""{"principals":[{{Fixtures.Principal("principal:alex")}}],"page":{{Fixtures.Page()}}}"""
                : Fixtures.Principal("principal:alex")
            )
        );
        var client = TestClient.Create(handler);

        var all = await client.Principals.ListAllAsync();
        var principal = await client.Principals.GetAsync("principal:alex");
        var effective = await client.Principals.EffectiveAsync(
            "principal:alex",
            "team/eng",
            asOf: "2026-01-01T00:00:00Z"
        );
        await client.Principals.GrantAsync(
            "principal:alex",
            "team/eng/*",
            [Verbs.Read, Verbs.Write]
        );
        await client.Principals.RevokeAsync("principal:alex", "team/eng/*", [Verbs.Write]);

        all.Should().ContainSingle();
        principal.Grants["team/eng"].Should().Equal("read");
        effective.Verbs.Should().Equal("read");
        handler.Requests[2].UrlText.Should().Contain("/principals/principal%3Aalex/effective");
        handler.Requests[2].UrlText.Should().Contain("path=team%2Feng");
        handler.Requests[2].UrlText.Should().Contain("asOf=2026-01-01T00%3A00%3A00Z");
        handler.Requests[3].Method.Should().Be("POST");
        handler.Requests[3].UrlText.Should().EndWith("/principals/principal%3Aalex/grants");
        JsonAssert.Equivalent(
            handler.Requests[3].Json,
            """{"path":"team/eng/*","verbs":["read","write"]}"""
        );
        handler.Requests[4].Method.Should().Be("DELETE");
        JsonAssert.Equivalent(
            handler.Requests[4].Json,
            """{"path":"team/eng/*","verbs":["write"]}"""
        );
    }

    [Test]
    public async Task ScopesRegisterListDeleteAndForget()
    {
        var handler = new FakeHandler(request =>
            request.Method switch
            {
                "DELETE" => Responses.Empty(),
                "GET" => Responses.Json(
                    $$"""{"scopes":[{{Fixtures.ScopeNode("team/eng")}}],"page":{{Fixtures.Page()}}}"""
                ),
                _ when request.Path.EndsWith("/forget", StringComparison.Ordinal) => Responses.Json(
                    """{"forgotten":4}"""
                ),
                _ => Responses.Json(Fixtures.ScopeNode("team/eng")),
            }
        );
        var client = TestClient.Create(handler);

        await client.Scopes.RegisterAsync("team/eng", displayName: "Engineering");
        var page = await client.Scopes.ListAsync(new CursorOptions { Limit = 50, Cursor = "c" });
        await client.Scopes.DeleteAsync("team/eng");
        var forgotten = await client.Scopes.ForgetAsync("team/eng");

        JsonAssert.Equivalent(
            handler.Requests[0].Json,
            """{"path":"team/eng","displayName":"Engineering"}"""
        );
        page.Scopes.Should().ContainSingle();
        handler.Requests[1].UrlText.Should().Contain("limit=50").And.Contain("cursor=c");
        handler.Requests[2].UrlText.Should().EndWith("/api/v1/ctx-1/scopes?path=team%2Feng");
        forgotten.Forgotten.Should().Be(4);
        JsonAssert.Equivalent(handler.Requests[3].Json, """{"path":"team/eng"}""");
    }

    [Test]
    public async Task ScopesListNeverSendsCount()
    {
        // /scopes takes limit and cursor only.
        var handler = FakeHandler.Json($$"""{"scopes":[],"page":{{Fixtures.Page()}}}""");

        await TestClient
            .Create(handler)
            .Scopes.ListAsync(new PageOptions { Limit = 5, Count = true });

        handler.Last.UrlText.Should().Contain("limit=5").And.NotContain("count");
    }
}
