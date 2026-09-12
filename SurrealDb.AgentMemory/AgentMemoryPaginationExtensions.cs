using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Cursor-based pagination helpers for every Agent Memory list endpoint.
/// </summary>
public static class AgentMemoryPaginationExtensions
{
    /// <summary>
    /// Yields every page of a listing, following <c>page.nextCursor</c> to exhaustion.
    /// Terminates on the token (never on a short page) and fails loudly if the server
    /// repeats a cursor instead of advancing.
    /// </summary>
    /// <param name="api">The Agent Memory client (the walk itself is driven by <paramref name="fetchPage"/>).</param>
    /// <param name="fetchPage">
    /// Receives the cursor to resume from (<see langword="null"/> on the first call) and must
    /// pass it through to the underlying request unchanged, e.g.
    /// <c>(cursor, ct) => api.ListActionsAsync(context, cursor: cursor.ToOption(), cancellationToken: ct)</c>.
    /// </param>
    /// <param name="pageOf">Selects the <see cref="PageMeta"/> block from a response.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    public static async IAsyncEnumerable<TPage> WalkPagesAsync<TPage>(
        this DefaultApi api,
        Func<string?, CancellationToken, Task<TPage>> fetchPage,
        Func<TPage, PageMeta?> pageOf,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (api is null)
        {
            throw new ArgumentNullException(nameof(api));
        }
        if (fetchPage is null)
        {
            throw new ArgumentNullException(nameof(fetchPage));
        }
        if (pageOf is null)
        {
            throw new ArgumentNullException(nameof(pageOf));
        }

        string? cursor = null;
        var seen = new HashSet<string>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await fetchPage(cursor, cancellationToken).ConfigureAwait(false);
            yield return response;

            var meta = pageOf(response);
            var next = meta?.NextCursorOption.Value;

            // The token is the only reliable end-of-walk signal: absent (or empty) on the
            // last page. A page bounded in the database and then filtered for visibility
            // can return fewer rows than `limit` while more pages remain.
            if (string.IsNullOrEmpty(next))
            {
                yield break;
            }

            // A cursor that repeats means the server is not advancing; following it would
            // spin forever. Fail loudly rather than hang or truncate.
            if (!seen.Add(next))
            {
                throw new InvalidOperationException(
                    $"Agent Memory returned a repeated pagination cursor ('{next}'); the page walk cannot advance."
                );
            }

            cursor = next;
        }
    }

    /// <summary>
    /// Follows a listing's cursors to exhaustion and returns every row. This is an unbounded
    /// read by construction; prefer page-at-a-time <see cref="WalkPagesAsync"/> for anything
    /// user-facing and large. <paramref name="max"/> stops the walk once that many rows are in
    /// hand (the result can still overshoot by up to one page, because pages arrive whole).
    /// </summary>
    public static async Task<List<TRow>> CollectPagesAsync<TPage, TRow>(
        this DefaultApi api,
        Func<string?, CancellationToken, Task<TPage>> fetchPage,
        Func<TPage, PageMeta?> pageOf,
        Func<TPage, IEnumerable<TRow>?> rowsOf,
        int? max = null,
        CancellationToken cancellationToken = default
    )
    {
        if (api is null)
        {
            throw new ArgumentNullException(nameof(api));
        }
        if (rowsOf is null)
        {
            throw new ArgumentNullException(nameof(rowsOf));
        }

        var rows = new List<TRow>();

        await foreach (
            var page in WalkPagesAsync(api, fetchPage, pageOf, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            var items = rowsOf(page);
            if (items is not null)
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

/// <summary>
/// Bridges nullable reference values to the generated <see cref="Option{TType}"/> query-parameter
/// wrapper: a <see langword="null"/> cursor must map to an <i>unset</i> option (parameter omitted),
/// which the implicit <c>string? → Option&lt;string&gt;</c> conversion would not guarantee.
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// Converts a nullable value into an <see cref="Option{TType}"/> that is unset when the
    /// value is <see langword="null"/>, e.g. <c>cursor: cursor.ToOption()</c>.
    /// </summary>
    public static Option<T> ToOption<T>(this T? value)
        where T : class => value is null ? default : new Option<T>(value);
}
