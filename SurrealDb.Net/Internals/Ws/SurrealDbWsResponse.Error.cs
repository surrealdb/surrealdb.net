using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsErrorResponse : ISurrealDbWsStandardResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public SurrealDbWsErrorResponseContent Error { get; set; } = new();
}

internal class SurrealDbWsErrorResponseContent
{
    [JsonPropertyName("code")]
    [CborProperty("code")]
    public long Code { get; set; }

    [JsonPropertyName("message")]
    [CborProperty("message")]
    public string Message { get; set; } = string.Empty;
}
