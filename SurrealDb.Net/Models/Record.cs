using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// The base record type.
/// </summary>
public abstract class Record
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public Thing? Id { get; set; }
}
