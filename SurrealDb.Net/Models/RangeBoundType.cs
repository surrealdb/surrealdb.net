namespace SurrealDb.Net.Models;

/// <summary>
/// The type of <see cref="RangeBound{T}"/>. It can be either inclusive or exclusive.
/// </summary>
public enum RangeBoundType
{
    /// <summary>
    /// An infinite endpoint. Indicates that there is no bound in this direction.
    /// </summary>
    Unbounded = 0,

    /// <summary>
    /// An inclusive bound.
    /// </summary>
    Inclusive,

    /// <summary>
    /// An exclusive bound.
    /// </summary>
    Exclusive,
}
