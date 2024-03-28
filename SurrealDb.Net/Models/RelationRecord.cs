using SurrealDb.Net.Internals.Json.Converters;
using System.Text.Json.Serialization;

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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on client operations
    public Thing? In { get; set; }

    /// <summary>
    /// The id of the record the relation ends at
    /// </summary>
    [JsonConverter(typeof(ThingConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on client operations
    public Thing? Out { get; set; }
}
