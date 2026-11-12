using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using SurrealDb.AgentMemory.Model;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/memory.test.ts</c>, plus the other root operations of the client.
/// </summary>
public class MemoryTests
{
    [Test]
    public async Task RecallPostsACamelCaseBodyToQuery()
    {
        var handler = FakeHandler.Json(Fixtures.QueryMemoryResponse);
        var client = TestClient.Create(handler);

        var response = await client.RecallAsync(
            "hi",
            new RecallOptions { K = 3, SessionId = "s1" }
        );

        response.Hits.Should().BeEmpty();
        var request = handler.Last;
        request.Method.Should().Be("POST");
        request.UrlText.Should().Contain("/api/v1/ctx-1/query");
        JsonAssert.Equivalent(request.Json, """{"query":"hi","k":3,"sessionId":"s1"}""");
        // A read behind a POST, so it takes the retry budget every other read gets.
        request.Header("Idempotency-Key").Should().HaveLength(64);
    }

    [Test]
    public async Task RecallSendsEveryOptionTheEndpointAccepts()
    {
        var handler = FakeHandler.Json(Fixtures.QueryMemoryResponse);
        var client = TestClient.Create(handler);

        await client.RecallAsync(
            "where",
            new RecallOptions
            {
                K = 5,
                Mode = "hybrid",
                SessionId = "s1",
                Include = ["facts"],
                AsOf = "2026-01-01T00:00:00Z",
                AtInstant = "2026-01-02T00:00:00Z",
                Labels = ["team=eng"],
                Lens = "team/eng/*",
                ScopeView = ScopeViews.Strict,
                ValidFrom = "2025-01-01T00:00:00Z",
                ValidUntil = "2027-01-01T00:00:00Z",
                Source = "cli",
                Location = new GeoFilterJson(
                    near: new GeoNearJson(lat: 52.37, lng: 4.9, radiusKm: 2.5)
                ),
            }
        );

        JsonAssert.Equivalent(
            handler.Last.Json,
            """
            {"query":"where","k":5,"mode":"hybrid","sessionId":"s1","include":["facts"],
             "asOf":"2026-01-01T00:00:00Z","atInstant":"2026-01-02T00:00:00Z","labels":["team=eng"],
             "lens":[["team/eng/*"]],"scopeView":"strict","validFrom":"2025-01-01T00:00:00Z",
             "validUntil":"2027-01-01T00:00:00Z","source":"cli",
             "location":{"near":{"lat":52.37,"lng":4.9,"radiusKm":2.5}}}
            """
        );
    }

    [Test]
    public async Task RememberPostsASnakeCaseBodyToFactsWithAnIdempotencyKey()
    {
        var handler = FakeHandler.Json(Fixtures.FactsResponse);
        var client = TestClient.Create(handler);

        await client.RememberAsync(
            "I was promoted to CTO",
            new RememberOptions
            {
                SessionId = "s1",
                MemoryCategory = MemoryCategories.Identity,
                Scopes = "user/tobie",
            }
        );

        var request = handler.Last;
        request.Method.Should().Be("POST");
        request.Path.Should().Be("/api/v1/ctx-1/facts");
        JsonAssert.Equivalent(
            request.Json,
            """{"text":"I was promoted to CTO","session_id":"s1","memory_category":"identity","scopes":[["user/tobie"]]}"""
        );
        request.Header("Idempotency-Key").Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Test]
    public async Task RememberWritesCallerSuppliedTriplesWithoutText()
    {
        var handler = FakeHandler.Json(Fixtures.FactsResponse);
        var client = TestClient.Create(handler);

        await client.RememberAsync(
            options: new RememberOptions
            {
                Infer = InferModes.Triples,
                Role = TurnRoles.Assistant,
                Labels = ["source=crm"],
                Triples =
                [
                    new Triple(
                        entity: new TripleEntity(name: "tobie", type: "person"),
                        key: "employer",
                        value: "SurrealDB"
                    ),
                ],
            }
        );

        var body = handler.Last.Json!.AsObject();
        body.ContainsKey("text").Should().BeFalse();
        body["infer"]!.GetValue<string>().Should().Be("triples");
        body["role"]!.GetValue<string>().Should().Be("assistant");
        body["triples"]![0]!["entity"]!["name"]!.GetValue<string>().Should().Be("tobie");
        body["triples"]![0]!["key"]!.GetValue<string>().Should().Be("employer");
    }

    [Test]
    public async Task RememberManyPostsTheMessagesToTheBatchEndpoint()
    {
        var handler = FakeHandler.Json(Fixtures.FactsBatchResponse);
        var client = TestClient.Create(handler);

        var response = await client.RememberManyAsync(
            [
                new BatchMessage(content: "I moved to Lisbon", role: TurnRole.User),
                new BatchMessage(content: "Noted.", role: TurnRole.Assistant),
            ],
            new RememberManyOptions
            {
                SessionId = "s1",
                Extract = BatchExtractionModes.WholeConversation,
                Scopes = new[] { new[] { "team/eng", "org/acme" } },
            }
        );

        response.TurnIds.Should().Equal("turn:1");
        var request = handler.Last;
        request.Path.Should().Be("/api/v1/ctx-1/facts/batch");
        request.Header("Idempotency-Key").Should().HaveLength(64);
        JsonAssert.Equivalent(
            request.Json,
            """
            {"messages":[{"content":"I moved to Lisbon","role":"user"},{"content":"Noted.","role":"assistant"}],
             "session_id":"s1","extract":"whole_conversation","scopes":[["team/eng","org/acme"]]}
            """
        );
    }

    [Test]
    public async Task ContextPostsToContext()
    {
        var handler = FakeHandler.Json(Fixtures.ContextResponse);
        var client = TestClient.Create(handler);

        var response = await client.ContextAsync("q");

        response.Context.Should().Be("blob");
        handler.Last.UrlText.Should().EndWith("/api/v1/ctx-1/context");
    }

    [Test]
    public async Task SessionsCreatePostsToSessions()
    {
        var handler = FakeHandler.Json(Fixtures.SessionResponse);
        var client = TestClient.Create(handler);

        var session = await client.Sessions.CreateAsync(
            new SessionCreateOptions { Scopes = "u/1" }
        );

        session.Id.Should().Be("s1");
        session.Scopes.Should().ContainSingle().Which.Should().Equal("u/1");
        handler.Last.UrlText.Should().EndWith("/api/v1/ctx-1/sessions");
        JsonAssert.Equivalent(handler.Last.Json, """{"scopes":[["u/1"]]}""");
    }

    [Test]
    public async Task WhoamiGetsMe()
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var client = TestClient.Create(handler);

        var me = await client.WhoamiAsync();

        me.PrincipalId.Should().Be("principal:tobie");
        handler.Last.Method.Should().Be("GET");
        handler.Last.UrlText.Should().EndWith("/api/v1/ctx-1/me");
    }

    [Test]
    public async Task HealthGetsTheUnscopedHealthEndpoint()
    {
        var handler = new FakeHandler(_ => Responses.Text("", HttpStatusCode.OK));
        var client = TestClient.Create(handler);

        await client.HealthAsync();

        handler.Last.Method.Should().Be("GET");
        handler.Last.UrlText.Should().Be("https://api.test/api/v1/health");
    }

    [Test]
    public async Task HealthThrowsWhenTheServiceIsUnhealthy()
    {
        var client = TestClient.Create(
            FakeHandler.Json("{}", HttpStatusCode.ServiceUnavailable),
            options => options.MaxRetries = 0
        );

        var act = () => client.HealthAsync();

        await act.Should().ThrowAsync<AgentMemoryServerException>();
    }

    [Test]
    public async Task ForgetSendsPurgeOnlyWhenRequested()
    {
        var handler = FakeHandler.Json(Fixtures.ForgetResponse);
        var client = TestClient.Create(handler);

        var forgotten = await client.ForgetAsync("old project notes");
        await client.ForgetAsync("old project notes", new ForgetOptions { Purge = true });
        await client.ForgetAsync("old project notes", new ForgetOptions { Purge = false });

        forgotten.Deleted.Should().Be(3);
        JsonAssert.Equivalent(handler.Requests[0].Json, """{"query":"old project notes"}""");
        JsonAssert.Equivalent(
            handler.Requests[1].Json,
            """{"query":"old project notes","purge":true}"""
        );
        JsonAssert.Equivalent(handler.Requests[2].Json, """{"query":"old project notes"}""");
        // A write that is not idempotent carries no key and is not retried.
        handler.Requests[0].Header("Idempotency-Key").Should().BeNull();
    }

    [Test]
    public async Task ChatSendsOptInSwitchesOnlyWhenSet()
    {
        var handler = FakeHandler.Json(Fixtures.ChatResponse);
        var client = TestClient.Create(handler);

        var response = await client.ChatAsync(
            "What do you know about me?",
            new ChatOptions
            {
                SessionId = "s1",
                Scopes = new[] { "team/a", "team/b" },
                Model = "m",
                BypassCache = true,
                Labels = ["l=1"],
                SuppressMarkers = false,
            }
        );

        response.Reply.Should().Be("You are the CTO.");
        response.Citations.Should().ContainSingle().Which.Marker.Should().Be("S1");
        handler.Last.Path.Should().Be("/api/v1/ctx-1/chat");
        handler.Last.Header("Idempotency-Key").Should().BeNull();
        JsonAssert.Equivalent(
            handler.Last.Json,
            """{"message":"What do you know about me?","sessionId":"s1","scopes":[["team/a"],["team/b"]],"model":"m","bypassCache":true,"labels":["l=1"]}"""
        );
    }

    [Test]
    public async Task ReflectAlwaysSendsPersist()
    {
        var handler = FakeHandler.Json(Fixtures.ReflectResponse);
        var client = TestClient.Create(handler);

        await client.ReflectAsync("What changed this week?");
        await client.ReflectAsync("What changed this week?", new ReflectOptions { Persist = true });

        JsonAssert.Equivalent(
            handler.Requests[0].Json,
            """{"query":"What changed this week?","persist":false}"""
        );
        JsonAssert.Equivalent(
            handler.Requests[1].Json,
            """{"query":"What changed this week?","persist":true}"""
        );
    }

    [Test]
    public async Task MaintenanceOperationsSendAJsonObjectEvenWithoutOptions()
    {
        // The service requires a JSON body on these routes; the Python client regressed on this (#431).
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Path switch
                {
                    "/api/v1/ctx-1/consolidate" => Fixtures.ConsolidateResponse,
                    "/api/v1/ctx-1/elaborate" => Fixtures.ElaborateResponse,
                    "/api/v1/ctx-1/fsck" => Fixtures.FsckResponse,
                    "/api/v1/ctx-1/scopes/forget" => """{"forgotten":0}""",
                    "/api/v1/ctx-1/sessions" => Fixtures.SessionResponse,
                    _ => """{"affected":0}""",
                }
            )
        );
        var client = TestClient.Create(handler);

        await client.ConsolidateAsync();
        await client.ElaborateAsync();
        await client.FsckAsync();
        await client.Scopes.ForgetAsync();
        await client.Sessions.CreateAsync();
        await client.Lifecycle.ExpireAsync();
        await client.Lifecycle.DecayAsync();

        handler.Requests.Should().HaveCount(7);
        foreach (var request in handler.Requests)
        {
            request.Method.Should().Be("POST");
            request.Header("Content-Type").Should().Be("application/json");
            request.BodyText.Should().Be("{}");
        }
    }

    [Test]
    public async Task MaintenanceOperationsSendTheirOptions()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Path switch
                {
                    "/api/v1/ctx-1/consolidate" => Fixtures.ConsolidateResponse,
                    "/api/v1/ctx-1/elaborate" => Fixtures.ElaborateResponse,
                    _ => Fixtures.FsckResponse,
                }
            )
        );
        var client = TestClient.Create(handler);

        await client.ConsolidateAsync(
            new ConsolidateOptions
            {
                DryRun = true,
                FactLimit = 0,
                ObservationLimit = 10,
            }
        );
        await client.ElaborateAsync(
            new ElaborateOptions
            {
                EntityRef = "person:tobie",
                Budget = 0,
                Sweep = true,
                DryRun = false,
            }
        );
        await client.FsckAsync(
            new FsckOptions
            {
                Check = "duplicates",
                DuplicateThreshold = 0.9,
                MaxResults = 0,
            }
        );

        JsonAssert.Equivalent(
            handler.Requests[0].Json,
            """{"dryRun":true,"factLimit":0,"observationLimit":10}"""
        );
        JsonAssert.Equivalent(
            handler.Requests[1].Json,
            """{"entityRef":"person:tobie","budget":0,"sweep":true}"""
        );
        JsonAssert.Equivalent(
            handler.Requests[2].Json,
            """{"check":"duplicates","duplicateThreshold":0.9,"maxResults":0}"""
        );
    }

    [Test]
    public async Task InspectPassesTheReferenceAndTemporalFilters()
    {
        var handler = FakeHandler.Json(
            $$"""{"kind":"trace","trace":{{Fixtures.TraceRecord("trace:1")}}}"""
        );
        var client = TestClient.Create(handler);

        var response = await client.InspectAsync(
            "trace:1",
            new TemporalOptions { AsOf = "2026-01-01T00:00:00Z" }
        );

        response.InspectResponseJsonOneOf3!.Trace.Id.Should().Be("trace:1");
        handler.Last.Path.Should().Be("/api/v1/ctx-1/inspect");
        handler
            .Last.UrlText.Should()
            .Contain("ref=trace%3A1")
            .And.Contain("asOf=2026-01-01T00%3A00%3A00Z");
    }

    [Test]
    public async Task AuditPassesItsFiltersAndAuditAllWalksTheCursor()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.UrlText.Contains("cursor=c2", StringComparison.Ordinal)
                    ? $$"""{"rows":[{{Fixtures.AuditRow("trace:2")}}],"page":{{Fixtures.Page()}}}"""
                    : $$"""{"rows":[{{Fixtures.AuditRow("trace:1")}}],"page":{{Fixtures.Page(true, "c2")}}}"""
            )
        );
        var client = TestClient.Create(handler);

        await client.AuditAsync(
            new AuditOptions
            {
                Principal = "principal:tobie",
                Kind = "retrieval",
                Since = "2026-01-01T00:00:00Z",
                Limit = 50,
            }
        );
        var rows = await client.AuditAllAsync(new AuditFilters { Key = "ci" });

        handler.Requests[0].UrlText.Should().Contain("principal=principal%3Atobie");
        handler.Requests[0].UrlText.Should().Contain("kind=retrieval");
        handler.Requests[0].UrlText.Should().Contain("limit=50");
        handler.Requests[0].UrlText.Should().NotContain("count=");
        rows.Select(row => row.TraceId).Should().Equal("trace:1", "trace:2");
        handler.Requests[2].UrlText.Should().Contain("key=ci").And.Contain("cursor=c2");
    }

    [Test]
    public async Task StateAndProfileReadTheirSnapshots()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Path.EndsWith("/state", StringComparison.Ordinal)
                    ? Fixtures.StateResponse
                    : Fixtures.ProfileResponse
            )
        );
        var client = TestClient.Create(handler);

        var state = await client.StateAsync(limit: 500);
        await client.ProfileAsync();

        state.Truncated.Instructions.Should().BeTrue();
        handler.Requests[0].UrlText.Should().EndWith("/api/v1/ctx-1/state?limit=500");
        handler.Requests[1].UrlText.Should().EndWith("/api/v1/ctx-1/profile");
    }

    [Test]
    public async Task EncodesTheContextIdInEveryPath()
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var client = TestClient.Create(handler, context: "team a/b");

        await client.WhoamiAsync();

        handler.Last.UrlText.Should().Be("https://api.test/api/v1/team%20a%2Fb/me");
    }

    [Test]
    public void RejectsMissingOptions()
    {
        AgentMemoryClient Create(Action<AgentMemoryOptions> configure)
        {
            var options = new AgentMemoryOptions
            {
                Context = "c",
                ApiKey = "k",
                Endpoint = "https://api.test",
            };
            configure(options);
            return new AgentMemoryClient(options);
        }

        FluentActions
            .Invoking(() => Create(options => options.Context = ""))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*context*");
        FluentActions
            .Invoking(() => Create(options => options.ApiKey = ""))
            .Should()
            .Throw<ArgumentException>();
        FluentActions
            .Invoking(() => Create(options => options.Endpoint = "not a url"))
            .Should()
            .Throw<ArgumentException>();
        var valid = Create(_ => { });
        valid.ContextId.Should().Be("c");
    }

    [Test]
    public async Task ATimeoutBeyondTheTimerLimitMeansNoDeadline()
    {
        // TimeSpan.MaxValue used to make every request throw ArgumentOutOfRangeException.
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var client = TestClient.Create(handler, options => options.Timeout = TimeSpan.MaxValue);

        var me = await client.WhoamiAsync();

        me.PrincipalId.Should().Be("principal:tobie");
    }

    [Test]
    public void RejectsOptionsThatWouldFailEveryRequest()
    {
        AgentMemoryClient Create(Action<AgentMemoryOptions> configure)
        {
            var options = new AgentMemoryOptions
            {
                Context = "c",
                ApiKey = "k",
                Endpoint = "https://api.test",
            };
            configure(options);
            return new AgentMemoryClient(options);
        }

        FluentActions
            .Invoking(() => Create(options => options.Timeout = TimeSpan.Zero))
            .Should()
            .Throw<ArgumentOutOfRangeException>();
        FluentActions
            .Invoking(() => Create(options => options.MaxRetries = -1))
            .Should()
            .Throw<ArgumentOutOfRangeException>();
        FluentActions
            .Invoking(() => Create(options => options.ApiKey = "sp-key:1-secret\r\n"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*trailing newline*");
        FluentActions
            .Invoking(() => Create(options => options.ApiKey = "two words"))
            .Should()
            .Throw<ArgumentException>();
        Create(options => options.Timeout = Timeout.InfiniteTimeSpan).ContextId.Should().Be("c");
    }
}
