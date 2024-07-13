using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Http;

internal class SurrealDbHttpErrorResponse : ISurrealDbHttpResponse
{
    [JsonPropertyName("error")]
    public SurrealDbHttpErrorResponseContent Error { get; set; } = new();
}

internal class SurrealDbHttpErrorResponseContent
{
    [JsonPropertyName("code")]
    [CborProperty("code")]
    public long Code { get; set; }

    [JsonPropertyName("message")]
    [CborProperty("message")]
    public string Message { get; set; } = string.Empty;
}
