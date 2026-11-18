using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/lookup.test.ts</c>: lookup, the context export, and uncertainty.
/// </summary>
public class LookupTests
{
    [Test]
    public async Task LookupPostsWithAnIdempotencyKey()
    {
        var handler = FakeHandler.Json(
            Fixtures.Lookup(
                $$"""{"kind":"entity","confidence":1,"subject":{{Fixtures.EntityMatch("tobie")}}}"""
            )
        );

        var response = await TestClient.Create(handler).LookupAsync("who is tobie");

        var request = handler.Last;
        request.Method.Should().Be("POST");
        request.UrlText.Should().EndWith("/api/v1/ctx-1/lookup");
        JsonAssert.Equivalent(request.Json, """{"query":"who is tobie"}""");
        // A read behind a POST, so it takes the retry budget every other read gets.
        request.Header("Idempotency-Key").Should().NotBeNullOrEmpty();
        response.Resolution.ResolutionJsonOneOf!.Subject.Entity.Name.Should().Be("tobie");
    }

    [Test]
    public async Task LookupSendsEveryOptionTheEndpointAcceptsAndNothingElse()
    {
        var handler = FakeHandler.Json(Fixtures.Lookup("""{"kind":"topic"}"""));

        await TestClient
            .Create(handler)
            .LookupAsync(
                "acme",
                new LookupOptions
                {
                    Subject = "company/acme",
                    EntityType = "company",
                    AmbiguityMargin = 0.3,
                    Include = [LookupSections.Facts, LookupSections.Passages],
                    FactLimit = 20,
                    RelationLimit = 10,
                    EventLimit = 5,
                    PassageLimit = 3,
                    UncertaintyLimit = 2,
                }
            );

        JsonAssert.Equivalent(
            handler.Last.Json,
            """
            {"query":"acme","subject":"company/acme","entityType":"company","ambiguityMargin":0.3,
             "include":["facts","passages"],"factLimit":20,"relationLimit":10,"eventLimit":5,
             "passageLimit":3,"uncertaintyLimit":2}
            """
        );
    }

    [Test]
    public async Task ResolutionNarrowsOnKind()
    {
        var ambiguous =
            $$"""{"kind":"ambiguous","candidates":[{"entity":{{Fixtures.EntityDetail("atlas", "product")}},"score":0.8,"factCount":2,"distinguisher":"shipped in March"}]}""";
        var client = TestClient.Create(FakeHandler.Json(Fixtures.Lookup(ambiguous)));

        var response = await client.LookupAsync("atlas");

        response.Resolution.ResolutionJsonOneOf.Should().BeNull();
        response
            .Resolution.ResolutionJsonOneOf1!.Candidates[0]
            .Distinguisher.Should()
            .Be("shipped in March");
    }

    [Test]
    public async Task ResolutionReadsTheTopicAndEmptyAnswers()
    {
        var topic = await TestClient
            .Create(FakeHandler.Json(Fixtures.Lookup("""{"kind":"topic"}""")))
            .LookupAsync("databases");
        var empty = await TestClient
            .Create(
                FakeHandler.Json(
                    Fixtures.Lookup(
                        $$"""{"kind":"empty","nearest":[{{Fixtures.EntityMatch("toby")}}]}"""
                    )
                )
            )
            .LookupAsync("tobie");

        topic.Resolution.ResolutionJsonOneOf2.Should().NotBeNull();
        empty.Resolution.ResolutionJsonOneOf3!.Nearest[0].Entity.Name.Should().Be("toby");
    }

    [Test]
    public async Task ContextCarriesTheSubjectForTheCopyAsContextExport()
    {
        var handler = FakeHandler.Json(Fixtures.ContextResponse);

        await TestClient
            .Create(handler)
            .ContextAsync("recent work", new ContextOptions { Subject = "person/tobie" });

        JsonAssert.Equivalent(
            handler.Last.Json,
            """{"query":"recent work","subject":"person/tobie"}"""
        );
    }

    [Test]
    public async Task UncertaintyListGetsWithItsFilters()
    {
        var handler = FakeHandler.Json(
            $$"""{"unknowns":[{{Fixtures.Uncertainty("uncertainty:1")}}],"page":{{Fixtures.Page()}}}"""
        );

        var page = await TestClient
            .Create(handler)
            .Uncertainty.ListAsync(
                new UncertaintyListOptions
                {
                    Entity = "person/tobie",
                    Resolved = false,
                    Limit = 20,
                }
            );

        var url = handler.Last.UrlText;
        url.Should().Contain("/api/v1/ctx-1/uncertainty");
        url.Should().Contain("entity=person%2Ftobie");
        url.Should().Contain("resolved=false");
        url.Should().Contain("limit=20");
        page.Unknowns[0].Resolvable.Should().BeTrue();
    }

    [Test]
    public async Task UncertaintyCountAsksForOneRowAndReadsTheTotal()
    {
        var handler = FakeHandler.Json(
            $$"""{"unknowns":[{{Fixtures.Uncertainty("uncertainty:1")}}],"page":{{Fixtures.Page(true, null, 12)}}}"""
        );

        var total = await TestClient
            .Create(handler)
            .Uncertainty.CountAsync(new UncertaintyFilters { Resolved = false });

        handler.Last.UrlText.Should().Contain("limit=1");
        handler.Last.UrlText.Should().Contain("count=true");
        total.Should().Be(12);
    }

    [Test]
    public async Task UncertaintyCountFallsBackToTheRowsWhenThereIsNoTotal()
    {
        var handler = FakeHandler.Json(
            $$"""{"unknowns":[{{Fixtures.Uncertainty("uncertainty:1")}}],"page":{{Fixtures.Page()}}}"""
        );

        var total = await TestClient.Create(handler).Uncertainty.CountAsync();

        total.Should().Be(1);
    }

    [Test]
    public async Task UncertaintyListAllFollowsTheCursorToExhaustion()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.UrlText.Contains("cursor=", StringComparison.Ordinal)
                    ? $$"""{"unknowns":[{{Fixtures.Uncertainty("uncertainty:2")}}],"page":{{Fixtures.Page()}}}"""
                    : $$"""{"unknowns":[{{Fixtures.Uncertainty("uncertainty:1")}}],"page":{{Fixtures.Page(true, "c2")}}}"""
            )
        );

        var rows = await TestClient
            .Create(handler)
            .Uncertainty.ListAllAsync(new UncertaintyFilters { Resolved = false });

        rows.Select(row => row.Id).Should().Equal("uncertainty:1", "uncertainty:2");
        handler.Requests[1].UrlText.Should().Contain("cursor=c2");
    }

    [Test]
    public async Task UncertaintyResolvePostsTheAcceptedValueAndUnwrapsTheSettledFlag()
    {
        var handler = FakeHandler.Json(
            $$"""{"uncertainty":{{Fixtures.Uncertainty("uncertainty:1", resolved: true, resolvable: false)}}}"""
        );

        var settled = await TestClient
            .Create(handler)
            .Uncertainty.ResolveAsync(
                "uncertainty:1",
                "SurrealDB",
                new ResolveUncertaintyOptions { Note = "confirmed in the offer letter" }
            );

        handler.Last.Method.Should().Be("POST");
        handler.Last.UrlText.Should().EndWith("/api/v1/ctx-1/uncertainty/uncertainty%3A1/resolve");
        JsonAssert.Equivalent(
            handler.Last.Json,
            """{"acceptedValue":"SurrealDB","note":"confirmed in the offer letter"}"""
        );
        settled.Resolved.Should().BeTrue();
    }

    [Test]
    public async Task UncertaintyResolveOmitsAnAbsentNoteRatherThanSendingNull()
    {
        var handler = FakeHandler.Json(
            $$"""{"uncertainty":{{Fixtures.Uncertainty("uncertainty:1", resolved: true)}}}"""
        );

        await TestClient.Create(handler).Uncertainty.ResolveAsync("uncertainty:1", "SurrealDB");

        JsonAssert.Equivalent(handler.Last.Json, """{"acceptedValue":"SurrealDB"}""");
    }
}
