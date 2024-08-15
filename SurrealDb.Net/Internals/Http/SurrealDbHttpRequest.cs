using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Http;

internal class SurrealDbHttpRequest
{
    [JsonPropertyName("method")]
    [CborProperty("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    [CborProperty("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [CborIgnoreIfDefault]
    public object?[]? Parameters { get; set; }
}
