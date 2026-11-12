using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/facts.test.ts</c>.
/// </summary>
public class FactsTests
{
    [Test]
    public async Task AttributesGetsWithItsFiltersAndPagination()
    {
        var handler = FakeHandler.Json($$"""{"attributes":[],"page":{{Fixtures.Page()}}}""");

        await TestClient
            .Create(handler)
            .Facts.AttributesAsync(
                new AttributeListOptions
                {
                    Entity = "person/tobie",
                    Key = "employer",
                    Limit = 50,
                    Count = true,
                }
            );

        var url = handler.Last.UrlText;
        url.Should().Contain("/api/v1/ctx-1/attributes");
        url.Should().Contain("entity=person%2Ftobie");
        url.Should().Contain("key=employer");
        url.Should().Contain("limit=50");
        url.Should().Contain("count=true");
    }

    [Test]
    public async Task AllAttributesFollowsTheCursorToExhaustion()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.UrlText.Contains("cursor=", StringComparison.Ordinal)
                    ? $$"""{"attributes":[{{Fixtures.Attribute("attribute:2")}}],"page":{{Fixtures.Page()}}}"""
                    : $$"""{"attributes":[{{Fixtures.Attribute("attribute:1")}}],"page":{{Fixtures.Page(true, "c2")}}}"""
            )
        );

        var rows = await TestClient
            .Create(handler)
            .Facts.AllAttributesAsync(new AttributeFilters { Entity = "person/tobie" });

        rows.Select(row => row.Id).Should().Equal("attribute:1", "attribute:2");
        handler.Requests[1].UrlText.Should().Contain("cursor=c2");
        handler
            .Requests.Should()
            .OnlyContain(request => request.UrlText.Contains("entity=person%2Ftobie"));
    }

    [Test]
    public async Task RelationsGetsWithSrcDstAndLabel()
    {
        var handler = FakeHandler.Json($$"""{"relations":[],"page":{{Fixtures.Page()}}}""");

        await TestClient
            .Create(handler)
            .Facts.RelationsAsync(
                new RelationListOptions
                {
                    Src = "person/tobie",
                    Dst = "company/acme",
                    Label = "works_at",
                }
            );

        var url = handler.Last.UrlText;
        url.Should().Contain("/api/v1/ctx-1/relations");
        url.Should().Contain("src=person%2Ftobie");
        url.Should().Contain("dst=company%2Facme");
        url.Should().Contain("label=works_at");
    }

    [Test]
    public async Task AllEdgesOfWalksBothDirectionsAndConcatenatesOutboundFirst()
    {
        var handler = new FakeHandler(request =>
        {
            var outbound = request.UrlText.Contains("src=", StringComparison.Ordinal);
            return Responses.Json(
                $$"""{"relations":[{{Fixtures.Relation(outbound ? "relation:out" : "relation:in")}}],"page":{{Fixtures.Page()}}}"""
            );
        });

        var edges = await TestClient.Create(handler).Facts.AllEdgesOfAsync("person/tobie");

        edges.Select(edge => edge.Id).Should().Equal("relation:out", "relation:in");
        handler
            .Requests.Should()
            .Contain(request => request.UrlText.Contains("src=person%2Ftobie"));
        handler
            .Requests.Should()
            .Contain(request => request.UrlText.Contains("dst=person%2Ftobie"));
    }

    [Test]
    public async Task ActionsGetsWithTheEventTimeBounds()
    {
        var handler = FakeHandler.Json($$"""{"actions":[],"page":{{Fixtures.Page()}}}""");

        await TestClient
            .Create(handler)
            .Facts.ActionsAsync(
                new ActionListOptions
                {
                    Actor = "person/tobie",
                    Verb = "shipped",
                    Since = "2026-01-01T00:00:00Z",
                    Until = "2026-06-01T00:00:00Z",
                }
            );

        var url = handler.Last.UrlText;
        url.Should().Contain("/api/v1/ctx-1/actions");
        url.Should().Contain("actor=person%2Ftobie");
        url.Should().Contain("verb=shipped");
        url.Should().Contain("since=2026-01-01T00%3A00%3A00Z");
        url.Should().Contain("until=2026-06-01T00%3A00%3A00Z");
    }

    [Test]
    public async Task AllActionsStopsOnceMaxRowsAreInHand()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                $$"""{"actions":[{{Fixtures.Action("action:" + request.UrlText.Length)}}],"page":{{Fixtures.Page(true, "c" + request.UrlText.Length)}}}"""
            )
        );

        var rows = await TestClient.Create(handler).Facts.AllActionsAsync(max: 2);

        rows.Should().HaveCount(2);
        handler.Requests.Should().HaveCount(2);
    }
}
