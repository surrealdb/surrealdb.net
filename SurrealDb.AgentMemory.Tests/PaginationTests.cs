using FluentAssertions;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// The cursor walk behind every <c>ListAll</c> helper (<c>walkPages</c> / <c>collectPages</c> in the JavaScript client).
/// </summary>
public class PaginationTests
{
    private sealed record TestPage(List<int> Rows, PageMeta? Page);

    private static Func<string?, CancellationToken, Task<TestPage?>> Pages(
        List<string?> cursorsSeen,
        params TestPage?[] pages
    )
    {
        var index = 0;
        return (cursor, _) =>
        {
            cursorsSeen.Add(cursor);
            return Task.FromResult(pages[index++]);
        };
    }

    private static PageMeta Meta(string? next)
    {
        return new PageMeta(hasMore: next is not null, nextCursor: next);
    }

    [Test]
    public async Task FollowsTheCursorToExhaustion()
    {
        var cursors = new List<string?>();

        var rows = await AgentMemoryPagination.CollectPagesAsync(
            Pages(cursors, new([1, 2], Meta("c2")), new([3], Meta("c3")), new([4], Meta(null))),
            page => page.Page,
            page => page.Rows
        );

        rows.Should().Equal(1, 2, 3, 4);
        cursors.Should().Equal(null, "c2", "c3");
    }

    [Test]
    public async Task EndsOnTheCursorNotOnAShortPage()
    {
        // A page bounded in the database and then filtered for visibility can be short, or empty, mid-walk.
        var cursors = new List<string?>();

        var rows = await AgentMemoryPagination.CollectPagesAsync(
            Pages(cursors, new([], Meta("c2")), new([7], Meta(null))),
            page => page.Page,
            page => page.Rows
        );

        rows.Should().Equal(7);
    }

    [Test]
    public async Task TreatsAnEmptyCursorOrAMissingPageBlockAsTheEnd()
    {
        var emptyCursor = await AgentMemoryPagination.CollectPagesAsync(
            Pages([], new TestPage([1], new PageMeta(hasMore: true, nextCursor: ""))),
            page => page.Page,
            page => page.Rows
        );
        var noPage = await AgentMemoryPagination.CollectPagesAsync(
            Pages([], new TestPage([2], null)),
            page => page.Page,
            page => page.Rows
        );

        emptyCursor.Should().Equal(1);
        noPage.Should().Equal(2);
    }

    [Test]
    public async Task EndsTheWalkOnAnEmptyBody()
    {
        var rows = await AgentMemoryPagination.CollectPagesAsync(
            Pages([], new([1], Meta("c2")), null),
            page => page.Page,
            page => page.Rows
        );

        rows.Should().Equal(1);
    }

    [Test]
    public async Task FailsLoudlyOnARepeatedCursor()
    {
        var act = () =>
            AgentMemoryPagination.CollectPagesAsync(
                Pages([], new([1], Meta("c2")), new([2], Meta("c3")), new([3], Meta("c2"))),
                page => page.Page,
                page => page.Rows,
                "keys"
            );

        var error = await act.Should().ThrowAsync<InvalidOperationException>();
        error.Which.Message.Should().Contain("repeated pagination cursor").And.Contain("`keys`");
    }

    [Test]
    public async Task StopsOnceMaxRowsAreInHandOvershootingByAtMostOnePage()
    {
        var cursors = new List<string?>();

        var rows = await AgentMemoryPagination.CollectPagesAsync(
            Pages(cursors, new([1, 2], Meta("c2")), new([3, 4], Meta("c3")), new([5], Meta(null))),
            page => page.Page,
            page => page.Rows,
            max: 3
        );

        rows.Should().Equal(1, 2, 3, 4);
        cursors.Should().HaveCount(2);
    }

    [Test]
    public async Task WalkPagesYieldsWholePages()
    {
        var pages = new List<TestPage>();

        await foreach (
            var page in AgentMemoryPagination.WalkPagesAsync(
                Pages([], new([1, 2], Meta("c2")), new([3], Meta(null))),
                page => page.Page
            )
        )
        {
            pages.Add(page);
        }

        pages.Select(page => page.Rows.Count).Should().Equal(2, 1);
    }

    [Test]
    public async Task HonoursCancellationBetweenPages()
    {
        using var cancellation = new CancellationTokenSource();
        var fetches = 0;

        var act = async () =>
        {
            await foreach (
                var _ in AgentMemoryPagination.WalkPagesAsync<TestPage>(
                    (_, _) =>
                    {
                        fetches++;
                        cancellation.Cancel();
                        return Task.FromResult<TestPage?>(new([1], Meta($"c{fetches}")));
                    },
                    page => page.Page,
                    cancellationToken: cancellation.Token
                )
            ) { }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        fetches.Should().Be(1);
    }
}
