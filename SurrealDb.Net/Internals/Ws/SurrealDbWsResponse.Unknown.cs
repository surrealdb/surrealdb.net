using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsUnknownResponse : ISurrealDbWsStandardResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
