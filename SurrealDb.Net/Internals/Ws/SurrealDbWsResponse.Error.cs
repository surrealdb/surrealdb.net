using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsErrorResponse : ISurrealDbWsResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("error")]
	public SurrealDbWsErrorResponseContent Error { get; set; } = new();
}

internal class SurrealDbWsErrorResponseContent
{
	[JsonPropertyName("code")]
	public long Code { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; } = string.Empty;
}
