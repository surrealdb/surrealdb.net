using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/entities.test.ts</c>, plus listing, counting and option validation.
/// </summary>
public class EntitiesTests
{
    [Test]
    public async Task SearchGetsTheQueryAndUnwrapsMatches()
    {
        var handler = FakeHandler.Json($$"""{"matches":[{{Fixtures.EntityMatch("tobie")}}]}""");

        var matches = await TestClient
            .Create(handler)
            .Entities.SearchAsync("tobie", new EntitySearchOptions { Type = "person", Limit = 5 });

        handler.Last.Method.Should().Be("GET");
        var url = handler.Last.UrlText;
        url.Should().Contain("/api/v1/ctx-1/entities/search");
        url.Should().Contain("q=tobie");
        url.Should().Contain("type=person");
        url.Should().Contain("limit=5");
        matches.Should().ContainSingle().Which.Entity.Name.Should().Be("tobie");
    }

    [Test]
    public async Task SearchSendsNoFiltersTheCallerDidNotSet()
    {
        var handler = FakeHandler.Json("""{"matches":[]}""");

        await TestClient.Create(handler).Entities.SearchAsync("acme");

        handler.Last.UrlText.Should().Contain("q=acme");
        handler.Last.UrlText.Should().NotContain("type=");
        handler.Last.UrlText.Should().NotContain("limit=");
    }

    [Test]
    public async Task TopGetsTheRankedHeadAndUnwrapsEntities()
    {
        var handler = FakeHandler.Json(
            $$"""{"entities":[{{Fixtures.EntityMatch("acme", "company")}}]}"""
        );

        var top = await TestClient
            .Create(handler)
            .Entities.TopAsync(
                new TopEntitiesOptions { By = EntityRankings.Importance, Limit = 3 }
            );

        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/entities/top");
        handler.Last.UrlText.Should().Contain("by=importance");
        handler.Last.UrlText.Should().Contain("limit=3");
        top[0].Entity.Name.Should().Be("acme");
    }

    [Test]
    public async Task TopLeavesTheOrderingToTheServerWhenNoneIsGiven()
    {
        var handler = FakeHandler.Json("""{"entities":[]}""");

        await TestClient.Create(handler).Entities.TopAsync();

        handler.Last.UrlText.Should().EndWith("/api/v1/ctx-1/entities/top");
    }

    [Test]
    public async Task GetPassesTheBoundAndTheTemporalFiltersThrough()
    {
        var handler = FakeHandler.Json(Fixtures.EntityResponse(attributesTruncated: true));

        var response = await TestClient
            .Create(handler)
            .Entities.GetAsync(
                "person",
                "tobie",
                new EntityGetOptions
                {
                    Limit = 100,
                    AsOf = "2026-01-01T00:00:00Z",
                    ValidFrom = "2025-01-01T00:00:00Z",
                }
            );

        var url = handler.Last.UrlText;
        url.Should().Contain("/api/v1/ctx-1/entities/person/tobie");
        url.Should().Contain("limit=100");
        url.Should().Contain("asOf=2026-01-01T00%3A00%3A00Z");
        url.Should().Contain("validFrom=2025-01-01T00%3A00%3A00Z");
        url.Should().NotContain("atInstant");
        response.Truncated.Attributes.Should().BeTrue();
    }

    [Test]
    public async Task GetUrlEncodesTheTypeAndNameSegments()
    {
        var handler = FakeHandler.Json(Fixtures.EntityResponse(attributesTruncated: false));

        await TestClient.Create(handler).Entities.GetAsync("person", "a/b");

        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/entities/person/a%2Fb");
    }

    [Test]
    public async Task NeighboursGetsTheNeighbourhoodWithItsFilterAndPagination()
    {
        var handler = FakeHandler.Json(
            $$"""{"neighbours":[{{Fixtures.Neighbour("works_at", true, "acme", "relation:1")}}],"page":{{Fixtures.Page()}}}"""
        );

        var page = await TestClient
            .Create(handler)
            .Entities.NeighboursAsync(
                "person",
                "tobie",
                new NeighbourhoodOptions { MinFacts = 2, Limit = 25 }
            );

        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/entities/person/tobie/neighbourhood");
        handler.Last.UrlText.Should().Contain("minFacts=2");
        handler.Last.UrlText.Should().Contain("limit=25");
        page.Neighbours[0].Far.Entity.Name.Should().Be("acme");
    }

    [Test]
    public async Task NeighboursRefusesCountTogetherWithMinFacts()
    {
        // The server rejects the pairing with a 400; the JavaScript client refuses it at the type level.
        var handler = FakeHandler.Json("{}");

        var act = () =>
            TestClient
                .Create(handler)
                .Entities.NeighboursAsync(
                    "person",
                    "tobie",
                    new NeighbourhoodOptions { MinFacts = 2, Count = true }
                );

        await act.Should().ThrowAsync<ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task AllNeighboursFollowsTheCursorToExhaustion()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.UrlText.Contains("cursor=", StringComparison.Ordinal)
                    ? $$"""{"neighbours":[{{Fixtures.Neighbour("knows", false, "alex", "relation:2")}}],"page":{{Fixtures.Page()}}}"""
                    : $$"""{"neighbours":[{{Fixtures.Neighbour("works_at", true, "acme", "relation:1")}}],"page":{{Fixtures.Page(true, "c2")}}}"""
            )
        );

        var rows = await TestClient.Create(handler).Entities.AllNeighboursAsync("person", "tobie");

        rows.Select(row => row.Far.Entity.Name).Should().Equal("acme", "alex");
        handler.Requests[0].UrlText.Should().NotContain("cursor=");
        handler.Requests[1].UrlText.Should().Contain("cursor=c2");
    }

    [Test]
    public async Task ChangesGetsTheAllKeysChainWithPagination()
    {
        var handler = FakeHandler.Json($$"""{"history":[],"page":{{Fixtures.Page()}}}""");

        await TestClient
            .Create(handler)
            .Entities.ChangesAsync("person", "tobie", new PageOptions { Limit = 50, Count = true });

        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/entities/person/tobie/history");
        handler.Last.UrlText.Should().Contain("limit=50");
        handler.Last.UrlText.Should().Contain("count=true");
    }

    [Test]
    public async Task HistoryReadsOneKeyAndUnwrapsItsChain()
    {
        var handler = FakeHandler.Json($$"""{"history":[{{Fixtures.Attribute("attribute:1")}}]}""");

        var rows = await TestClient
            .Create(handler)
            .Entities.HistoryAsync("person", "tobie", "employer");

        handler
            .Last.UrlText.Should()
            .Contain("/api/v1/ctx-1/entities/person/tobie/history/employer");
        rows.Should().HaveCount(1);
    }

    [Test]
    public async Task ListCountAndDeleteUseTheCollectionRoutes()
    {
        var handler = new FakeHandler(request =>
            request.Method == "DELETE"
                ? Responses.Empty()
                : Responses.Json(
                    $$"""{"entities":[{{Fixtures.EntityDetail("tobie")}}],"page":{{Fixtures.Page(true, "c2", 40)}}}"""
                )
        );
        var client = TestClient.Create(handler);

        var page = await client.Entities.ListAsync(
            new EntityListOptions { Type = "person", Limit = 1 }
        );
        var total = await client.Entities.CountAsync("person");
        await client.Entities.DeleteAsync("person", "tobie");

        page.Entities.Should().ContainSingle();
        handler.Requests[0].UrlText.Should().Contain("type=person").And.Contain("limit=1");
        total.Should().Be(40);
        handler.Requests[1].UrlText.Should().Contain("count=true");
        handler.Requests[2].Method.Should().Be("DELETE");
        handler.Requests[2].UrlText.Should().EndWith("/api/v1/ctx-1/entities/person/tobie");
    }
}
