using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Json.Converters;

namespace SurrealDb.Net.Models;

/// <summary>
/// The abstract class for Record type.
/// </summary>
public abstract class Record : IRecord
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [JsonConverter(typeof(ThingConverter))]
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on Create operation
    public Thing? Id { get; set; }
}

/// <summary>
/// The interface for Record type.
/// </summary>
public interface IRecord
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [JsonConverter(typeof(ThingConverter))]
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on Create operation
    public Thing? Id { get; set; }
}
