namespace SurrealDb.Net.Models;

/// <summary>
/// A placeholder type that reflects a Record ID stored a single string.
/// </summary>
///
/// <remarks>
/// For performance reasons, the parsing of Record IDs is not implemented in the library,
/// as that would require to parse any SurrealQL value in the provided <see cref="string"/>.
/// And for this reason, this type is only used for one-way communication,
/// sending a <see cref="StringRecordId"/> from the client to a SurrealDB instance.
/// This library will never be able to deserialize a <see cref="StringRecordId"/> into a <see cref="RecordId"/>.
/// As such, please consider using <see cref="RecordId"/> or one of its inherited types for these scenarii.
/// </remarks>
public readonly partial struct StringRecordId
    : IEquatable<StringRecordId>,
        IComparable<StringRecordId>
{
    /// <summary>
    /// The value representing the Record ID as a single string, e.g.
    /// <example>"person:john"</example>
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Instanciate a new <see cref="StringRecordId"/> that will contain a Record ID representation as a single string.
    /// </summary>
    /// <exception cref="InvalidOperationException">A value is required</exception>
    public StringRecordId()
    {
        throw new InvalidOperationException("A value is required");
    }

    /// <summary>
    /// Instanciate a new <see cref="StringRecordId"/> that will contain a Record ID representation as a single string.
    /// </summary>
    /// <param name="value">
    /// The value representing the Record ID as a single string, e.g.
    /// <example>"person:john"</example>
    /// </param>
    public StringRecordId(string value)
    {
        Value = value;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringRecordId recordId && Equals(recordId);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public bool Equals(StringRecordId other)
    {
        return Value == other.Value;
    }

    public int CompareTo(StringRecordId other)
    {
        return Value.CompareTo(other.Value);
    }
}
