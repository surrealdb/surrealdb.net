using System.Text.Json.Serialization;

namespace SurrealDb.Internals.Ws;

internal class SurrealDbWsUnknownResponse : ISurrealDbWsResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;
}
