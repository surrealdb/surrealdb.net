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
/// This library will never be able to deserialize a <see cref="StringRecordId"/> into a <see cref="Thing"/>.
/// As such, please consider using <see cref="Thing"/> or one of its inherited types for these scenarii.
/// </remarks>
public readonly struct StringRecordId
{
    /// <summary>
    /// The value representing the Record ID as a single string, e.g.
    /// <example>"person:john"</example>
    /// </summary>
    public string Value { get; }

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
}
