namespace SurrealDb.Net.Models;

/// <summary>
/// A <see cref="RangeBound{T}"/> used exclusively for the <see cref="Range{TStart, TEnd}"/> type.
/// </summary>
/// <typeparam name="T">The data type of the bound value.</typeparam>
public readonly struct RangeBound<T>
{
    /// <summary>
    /// The type of the bound.
    /// </summary>
    public RangeBoundType Type { get; }

    /// <summary>
    /// The value of the bound.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Creates a new RangeBound, given a value and the type of the bound.
    /// </summary>
    /// <param name="value">The expected bound value.</param>
    /// <param name="type">The expected bound type.</param>
    public RangeBound(T value, RangeBoundType type)
    {
        Value = value;
        Type = type;
    }
}

public static class RangeBound
{
    /// <summary>
    /// Creates an inclusive <see cref="RangeBound{T}"/>.
    /// </summary>
    /// <typeparam name="T">The data type of the bound value.</typeparam>
    /// <param name="value">The expected bound value.</param>
    public static RangeBound<T> Inclusive<T>(T value)
    {
        return new(value, RangeBoundType.Inclusive);
    }

    /// <summary>
    /// Creates an exclusive <see cref="RangeBound{T}"/>.
    /// </summary>
    /// <typeparam name="T">The data type of the bound value.</typeparam>
    /// <param name="value">The expected bound value.</param>
    public static RangeBound<T> Exclusive<T>(T value)
    {
        return new(value, RangeBoundType.Exclusive);
    }
}
