using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Internals.Json.Converters;

namespace SurrealDb.Net.Models;

/// <summary>
/// The base record type.
/// </summary>
public abstract class Record
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [JsonConverter(typeof(ThingConverter))]
    [JsonPropertyName("id")]
    [CborProperty("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on Create operation
    [CborIgnoreIfDefault]
    public Thing? Id { get; set; }
}
