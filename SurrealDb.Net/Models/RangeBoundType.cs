namespace SurrealDb.Net.Models;

/// <summary>
/// The type of <see cref="RangeBound{T}"/>. It can be either inclusive or exclusive.
/// </summary>
public enum RangeBoundType
{
    Unknown = 0,
    Inclusive,
    Exclusive,
}
