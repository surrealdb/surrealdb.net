using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsRequest
{
    [JsonPropertyName("id")]
    [CborProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    [CborProperty("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    [CborProperty("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [CborIgnoreIfDefault]
    public object?[]? Parameters { get; set; }
}
