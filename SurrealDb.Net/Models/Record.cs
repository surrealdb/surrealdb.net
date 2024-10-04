using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Json;

namespace SurrealDb.Net.Models;

/// <summary>
/// The base record type.
/// </summary>
public abstract class Record
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter))]
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }
}
