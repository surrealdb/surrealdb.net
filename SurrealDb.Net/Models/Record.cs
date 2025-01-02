using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Json;

namespace SurrealDb.Net.Models;

/// <summary>
/// The interface that describes record type.
/// </summary>
public interface IRecord
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }
}

/// <summary>
/// The base record type.
/// </summary>
public abstract class Record : IRecord
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }
}
