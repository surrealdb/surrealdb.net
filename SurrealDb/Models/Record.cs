using SurrealDb.Internals.Json.Converters;
using System.Text.Json.Serialization;

namespace SurrealDb.Models;

/// <summary>
/// The base record type.
/// </summary>
public abstract class Record
{
	/// <summary>
	/// The id of the record
	/// </summary>
	[JsonConverter(typeof(ThingConverter))]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // 💡 ignore null value to prevent failure on Create operation
	public Thing? Id { get; set; }
}
