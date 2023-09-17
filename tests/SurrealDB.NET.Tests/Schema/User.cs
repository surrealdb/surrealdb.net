using System.Text.Json.Serialization;
using SurrealDB.NET.Json;

namespace SurrealDB.NET.Tests.Schema;

internal sealed record User(
	[property: JsonPropertyName("email")]
	string Email,
	[property: JsonPropertyName("password")]
	string Password)
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonConverter(typeof(SurrealThingJsonConverter))]
	public Thing Id { get; init; }
};
