using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Json;

namespace SurrealDb.Net.Models;

/// <summary>
/// The interface that describes relation record type.
/// </summary>
public interface IRelationRecord : IRecord
{
    /// <summary>
    /// The id of the record the relation starts from
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("in")]
    [CborIgnoreIfDefault]
    public RecordId? In { get; set; }

    /// <summary>
    /// The id of the record the relation ends at
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("out")]
    [CborIgnoreIfDefault]
    public RecordId? Out { get; set; }
}

/// <summary>
/// The base relation record type.
/// </summary>
public abstract class RelationRecord : Record, IRelationRecord
{
    /// <summary>
    /// The id of the record the relation starts from
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("in")]
    [CborIgnoreIfDefault]
    public RecordId? In { get; set; }

    /// <summary>
    /// The id of the record the relation ends at
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("out")]
    [CborIgnoreIfDefault]
    public RecordId? Out { get; set; }
}
