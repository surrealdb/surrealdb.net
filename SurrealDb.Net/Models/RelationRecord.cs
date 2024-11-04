using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// The base relation record type.
/// </summary>
public abstract class RelationRecord : Record
{
    /// <summary>
    /// The id of the record the relation starts from
    /// </summary>
    [CborProperty("in")]
    [CborIgnoreIfDefault]
    public RecordId? In { get; set; }

    /// <summary>
    /// The id of the record the relation ends at
    /// </summary>
    [CborProperty("out")]
    [CborIgnoreIfDefault]
    public RecordId? Out { get; set; }
}
