namespace SurrealDb.Net.Models;

/// <summary>
/// This represents a <c>RecordIdRange</c> type that is composed of a table name and a <see cref="Range{TStart, TEnd}"/>.
/// </summary>
/// <typeparam name="TStart">The value type of the left/start bound.</typeparam>
/// <typeparam name="TEnd">The value type of the right/end bound.</typeparam>
public readonly struct RecordIdRange<TStart, TEnd>
{
    /// <summary>
    /// The table name.
    /// </summary>
    public string Table { get; }

    /// <summary>
    /// The range used to limit search for the id part of a <see cref="RecordId"/>.
    /// </summary>
    public Range<TStart, TEnd> Range { get; }

    /// <summary>
    /// Creates a new RecordIdRange, given the start and end bounds.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="start">The left/start range bound.</param>
    /// <param name="end">The right/end range bound.</param>
    /// <exception cref="ArgumentNullException">A table name should be provided.</exception>
    public RecordIdRange(string table, RangeBound<TStart> start, RangeBound<TEnd> end)
    {
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));

        Table = table;
        Range = new(start, end);
    }

    /// <summary>
    /// Creates a new RecordIdRange, given an existing range.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="range">The range.</param>
    /// <exception cref="ArgumentNullException">A table name should be provided.</exception>
    /// <exception cref="ArgumentException">The range must be valid.</exception>
    public RecordIdRange(string table, Range<TStart, TEnd> range)
    {
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));

        if (!range.Start.HasValue)
            throw new ArgumentException("The start part is not valid", nameof(range));

        if (!range.End.HasValue)
            throw new ArgumentException("The end part is not valid", nameof(range));

        Table = table;
        Range = range;
    }
}
