using Range = SurrealDb.Net.Models.Range;
using SystemRange = System.Range;

namespace SurrealDb.Net.Models;

/// <summary>
/// This represents a <c>Range</c> type that is composed of possible delimiters
/// known as <see cref="Range{TStart, TEnd}.Start"/> and <see cref="Range{TStart, TEnd}.End"/>.
/// </summary>
/// <typeparam name="TStart">The value type of the left/start bound.</typeparam>
/// <typeparam name="TEnd">The value type of the right/end bound.</typeparam>
public readonly struct Range<TStart, TEnd>
{
    /// <summary>
    /// The left/start bound of the range.
    /// </summary>
    public RangeBound<TStart>? Start { get; }

    /// <summary>
    /// The right/end bound of the range.
    /// </summary>
    public RangeBound<TEnd>? End { get; }

    /// <summary>
    /// Creates a full Range, that has no defined delimiters.
    /// </summary>
    public Range() { }

    /// <summary>
    /// Creates a new Range, given the start and end range.
    /// </summary>
    /// <param name="start">The expected left/start bound.</param>
    /// <param name="end">The expected right/end bound.</param>
    public Range(RangeBound<TStart>? start, RangeBound<TEnd>? end)
    {
        Start = start;
        End = end;
    }
}

public static class Range
{
    /// <summary>
    /// Creates a full <see cref="Range{TStart, TEnd}"/>, having no start or end delimiter.
    /// </summary>
    public static Range<None, None> Full()
    {
        return new(default, default);
    }

    /// <summary>
    /// Creates a partial <see cref="Range{TStart, TEnd}"/>, having a start bound limit but no end delimiter.
    /// </summary>
    /// <typeparam name="T">The value type of the left/start bound.</typeparam>
    /// <param name="start">The expected left/start bound.</param>
    public static Range<T, None> StartFrom<T>(RangeBound<T> start)
    {
        return new Range<T, None>(start, default);
    }

    /// <summary>
    /// Creates a partial <see cref="Range{TStart, TEnd}"/>, having an end bound limit but no start delimiter.
    /// </summary>
    /// <typeparam name="T">The value type of the right/end bound.</typeparam>
    /// <param name="end">The expected right/end bound.</param>
    public static Range<None, T> EndTo<T>(RangeBound<T> end)
    {
        return new Range<None, T>(default, end);
    }

    /// <summary>
    /// Creates a <see cref="Range{TStart, TEnd}"/> from a native <see cref="SystemRange"/> type.
    /// </summary>
    /// <param name="range">The native range to start from.</param>
    /// <exception cref="NotSupportedException">Failed to convert because one index of the range is 'fromEnd'.</exception>
    public static Range<int, int> FromRange(SystemRange range)
    {
        if (range.Equals(SystemRange.All))
        {
            return new Range<int, int>();
        }

        var withoutEndIndex = new Index(0, true);
        if (!range.Start.IsFromEnd && range.End.Equals(withoutEndIndex))
        {
            return new Range<int, int>(RangeBound.Inclusive(range.Start.Value), default);
        }

        var withoutStartIndex = new Index(0, false);
        if (!range.End.IsFromEnd && range.Start.Equals(withoutStartIndex))
        {
            return new Range<int, int>(default, RangeBound.Exclusive(range.End.Value));
        }

        if (range.Start.IsFromEnd || range.End.IsFromEnd)
        {
            throw new NotSupportedException(
                "Failed to convert because one index of the range is 'fromEnd'."
            );
        }

        return new Range<int, int>(
            RangeBound.Inclusive(range.Start.Value),
            RangeBound.Exclusive(range.End.Value)
        );
    }
}
