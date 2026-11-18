namespace SurrealDb.AgentMemory;

/// <summary>
/// Pagination for a listing that offers no total: <c>limit</c> and <c>cursor</c> only.
/// </summary>
/// <remarks>
/// <c>/audit</c> and <c>/scopes</c> take these two parameters only, so their <c>page.totalSize</c> is always absent.
/// </remarks>
public class CursorOptions
{
    /// <summary>
    /// Page size. Omitted means the server default (100); a value above the cap (500) is clamped rather than rejected.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Continuation token from the previous page's <c>page.nextCursor</c>.
    /// </summary>
    public string? Cursor { get; set; }
}

/// <summary>
/// The pagination parameters a countable list surface accepts.
/// </summary>
public class PageOptions : CursorOptions
{
    /// <summary>
    /// Also compute <c>page.totalSize</c>.
    /// Off by default, because it scans the whole filtered set.
    /// </summary>
    public bool? Count { get; set; }
}

/// <summary>
/// Cursor pagination, or the deprecated offset pagination that <c>/documents</c>, <c>/documents/{id}/chunks</c>,
/// and <c>/documents/keywords</c> still accept. Never both.
/// </summary>
/// <remarks>
/// The server rejects <see cref="CursorOptions.Cursor"/> sent together with <see cref="Page"/> with a <c>400</c>,
/// so the client refuses that pairing before sending it.
/// </remarks>
public class CursorOrOffsetOptions : PageOptions
{
    /// <summary>
    /// Zero-indexed page number. Deprecated: prefer <see cref="CursorOptions.Limit"/> and <see cref="CursorOptions.Cursor"/>.
    /// </summary>
    /// <remarks>
    /// Retained for callers with numbered page controls, which need a page index and a total
    /// that keyset pagination does not provide. They pay the scan cost the cursor path avoids.
    /// </remarks>
    public int? Page { get; set; }

    /// <summary>
    /// Rows per page, for <see cref="Page"/>. Deprecated: prefer <see cref="CursorOptions.Limit"/>.
    /// </summary>
    public int? PageSize { get; set; }

    internal void Validate()
    {
        if (Cursor is not null && Page is not null)
        {
            throw new ArgumentException(
                "Cursor and Page cannot be combined: the server rejects that pairing. Use one pagination mode."
            );
        }
    }
}
