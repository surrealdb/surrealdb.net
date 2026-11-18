using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/keys.test.ts</c>.
/// </summary>
public class KeysTests
{
    [Test]
    public async Task CreatePostsNameAndGrantsWithATtlQuery()
    {
        var handler = FakeHandler.Json(Fixtures.MintedKey("key:1", "sp-key:1-secret"));

        var minted = await TestClient
            .Create(handler)
            .Keys.CreateAsync(
                new KeyCreateOptions
                {
                    Name = "ci",
                    Grants = new Dictionary<string, IEnumerable<string>>
                    {
                        [Verbs.MemoryRead] = ["team/eng/*"],
                    },
                    TtlSeconds = 3600,
                }
            );

        minted.Id.Should().Be("key:1");
        minted.Key.Should().Be("sp-key:1-secret");
        var request = handler.Last;
        request.Method.Should().Be("POST");
        request.UrlText.Should().Contain("/api/v1/ctx-1/keys");
        request.UrlText.Should().Contain("ttlSeconds=3600");
        JsonAssert.Equivalent(
            request.Json,
            """{"name":"ci","grants":{"memory:read":["team/eng/*"]}}"""
        );
    }

    [Test]
    public async Task CreateOmitsTheBodyWhenNoNameOrGrantsAreGiven()
    {
        var handler = FakeHandler.Json(Fixtures.MintedKey("key:2", "sp-key:2-secret"));

        await TestClient.Create(handler).Keys.CreateAsync();

        handler.Last.Body.Should().BeNull();
        handler.Last.UrlText.Should().NotContain("ttlSeconds");
    }

    [Test]
    public async Task ListGetsKeysAndForwardsThePaginationParameters()
    {
        var handler = FakeHandler.Json($$"""{"keys":[],"page":{{Fixtures.Page()}}}""");

        var page = await TestClient
            .Create(handler)
            .Keys.ListAsync(new PageOptions { Limit = 25, Count = true });

        page.Keys.Should().BeEmpty();
        page.Page.HasMore.Should().BeFalse();
        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/keys");
        handler.Last.UrlText.Should().Contain("limit=25");
        handler.Last.UrlText.Should().Contain("count=true");
    }

    [Test]
    public async Task ListAllFollowsNextCursorToExhaustion()
    {
        var pages = new[]
        {
            $$"""{"keys":[{{Fixtures.KeyDetail("a")}}],"page":{{Fixtures.Page(true, "c1")}}}""",
            $$"""{"keys":[{{Fixtures.KeyDetail("b")}}],"page":{{Fixtures.Page()}}}""",
        };
        var calls = 0;
        var handler = new FakeHandler(_ => Responses.Json(pages[calls++]));

        var keys = await TestClient.Create(handler).Keys.ListAllAsync();

        keys.Select(key => key.Id).Should().Equal("a", "b");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].UrlText.Should().NotContain("cursor=");
        handler.Requests[1].UrlText.Should().Contain("cursor=c1");
    }

    [Test]
    public async Task ListAllDegradesToAnEmptyListOnAnEmptyBody()
    {
        var handler = new FakeHandler(_ => Responses.Empty());

        var keys = await TestClient.Create(handler).Keys.ListAllAsync();

        keys.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteDeletesThePathEncodedKey()
    {
        var handler = new FakeHandler(_ => Responses.Empty());

        await TestClient.Create(handler).Keys.DeleteAsync("key:1");

        handler.Last.Method.Should().Be("DELETE");
        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/keys/key%3A1");
    }

    [Test]
    public async Task RotatePostsWithATtlQuery()
    {
        var handler = FakeHandler.Json(Fixtures.MintedKey("key:1", "sp-key:1-rotated"));

        var minted = await TestClient.Create(handler).Keys.RotateAsync("key:1", ttlSeconds: 60);

        minted.Key.Should().Be("sp-key:1-rotated");
        handler.Last.Method.Should().Be("POST");
        handler.Last.UrlText.Should().Contain("/api/v1/ctx-1/keys/key%3A1/rotate");
        handler.Last.UrlText.Should().Contain("ttlSeconds=60");
    }

    [Test]
    public async Task CreateKeepsANanosecondExpiry()
    {
        // The generated converters used to drop a validUntil with nine fractional digits, which reads as "never expires".
        var handler = FakeHandler.Json(
            $$"""{"id":"key:3","key":"sp-key:3-secret","validUntil":"{{Fixtures.Timestamp}}"}"""
        );

        var minted = await TestClient.Create(handler).Keys.CreateAsync();

        minted
            .ValidUntil.Should()
            .Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1_234_567));
    }
}
