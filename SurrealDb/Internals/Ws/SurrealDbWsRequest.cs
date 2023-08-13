using System.Text.Json.Serialization;

namespace SurrealDb.Internals.Ws;

internal class SurrealDbWsRequest
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("method")]
	public string Method { get; set; } = string.Empty;

	[JsonPropertyName("params")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<object?>? Parameters { get; set; }
}
