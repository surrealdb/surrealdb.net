using System.Text.Json.Serialization;
using SurrealDB.NET.Json.Converters;

namespace SurrealDB.NET.Tests.Schema;

internal sealed record Post(
	[property: JsonPropertyName("content")]
	string Content)
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonConverter(typeof(SurrealThingJsonConverter))]
	public Thing Id { get; init; }

	[JsonPropertyName("tags")]
	public string? Tags { get; init; }
};
