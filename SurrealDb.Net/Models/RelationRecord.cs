using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Internals.Json.Converters;

namespace SurrealDb.Net.Models;

/// <summary>
/// The base relation record type.
/// </summary>
public abstract class RelationRecord : Record
{
    /// <summary>
    /// The id of the record the relation starts from
    /// </summary>
    [JsonConverter(typeof(ThingConverter))]
    [JsonPropertyName("in")]
    [CborProperty("in")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on client operations
    [CborIgnoreIfDefault]
    public Thing? In { get; set; }

    /// <summary>
    /// The id of the record the relation ends at
    /// </summary>
    [JsonConverter(typeof(ThingConverter))]
    [JsonPropertyName("out")]
    [CborProperty("out")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on client operations
    [CborIgnoreIfDefault]
    public Thing? Out { get; set; }
}
