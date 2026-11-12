using System.Runtime.CompilerServices;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Cursor-pagination helpers, for walking any Agent Memory list surface.
/// </summary>
public static class AgentMemoryPagination
{
    /// <summary>
    /// Yields every page of a listing, following <c>page.nextCursor</c> to exhaustion.
    /// </summary>
    /// <remarks>
    /// The walk ends on the cursor, never on a short page: a page bounded in the database and then filtered for
    /// visibility can return fewer rows than <c>limit</c> while more pages remain.
    /// An empty body (a <c>204</c>) carries no rows and no cursor, so it ends the walk rather than being yielded.
    /// </remarks>
    /// <param name="fetchPage">
    /// Receives the cursor to resume from (<see langword="null"/> on the first call) and must pass it to the
    /// underlying request unchanged.
    /// </param>
    /// <param name="pageOf">Selects the page block from a response.</param>
    /// <param name="collection">The collection name, for the error message.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    /// <exception cref="InvalidOperationException">The server repeated a cursor instead of advancing.</exception>
    public static async IAsyncEnumerable<TPage> WalkPagesAsync<TPage>(
        Func<string?, CancellationToken, Task<TPage?>> fetchPage,
        Func<TPage, PageMeta?> pageOf,
        string collection = "rows",
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TPage : class
    {
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(pageOf);

        string? cursor = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await fetchPage(cursor, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                yield break;
            }

            yield return response;

            var next = pageOf(response)?.NextCursor;
            if (string.IsNullOrEmpty(next))
            {
                yield break;
            }

            // A repeated cursor means the server is not advancing; following it would spin forever.
            if (!seen.Add(next))
            {
                throw new InvalidOperationException(
                    $"Agent Memory returned a repeated pagination cursor while walking `{collection}`; the page walk cannot advance."
                );
            }

            cursor = next;
        }
    }

    /// <summary>
    /// Follows a listing's cursors to exhaustion and returns every row.
    /// </summary>
    /// <remarks>
    /// Without <paramref name="max"/> this is an unbounded read by construction.
    /// <paramref name="max"/> stops the walk once that many rows are in hand;
    /// the result can still overshoot by up to one page, because pages arrive whole.
    /// </remarks>
    /// <param name="fetchPage">Fetches one page from a cursor.</param>
    /// <param name="pageOf">Selects the page block from a response.</param>
    /// <param name="rowsOf">Selects the rows from a response.</param>
    /// <param name="collection">The collection name, for the error message.</param>
    /// <param name="max">Stops the walk once this many rows are collected.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public static async Task<List<TRow>> CollectPagesAsync<TPage, TRow>(
        Func<string?, CancellationToken, Task<TPage?>> fetchPage,
        Func<TPage, PageMeta?> pageOf,
        Func<TPage, IEnumerable<TRow>?> rowsOf,
        string collection = "rows",
        int? max = null,
        CancellationToken cancellationToken = default
    )
        where TPage : class
    {
        ArgumentNullException.ThrowIfNull(rowsOf);

        var rows = new List<TRow>();
        await foreach (
            var page in WalkPagesAsync(fetchPage, pageOf, collection, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            if (rowsOf(page) is { } items)
            {
                rows.AddRange(items);
            }

            if (max is { } limit && rows.Count >= limit)
            {
                break;
            }
        }

        return rows;
    }
}
