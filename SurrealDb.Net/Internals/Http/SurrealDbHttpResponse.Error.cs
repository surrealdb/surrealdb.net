using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Http;

internal sealed class SurrealDbHttpErrorResponse : ISurrealDbHttpResponse
{
    public SurrealDbHttpErrorResponseContent Error { get; set; } = new();
}

internal sealed class SurrealDbHttpErrorResponseContent
{
    [CborProperty("code")]
    public long Code { get; set; }

    [CborProperty("message")]
    public string Message { get; set; } = string.Empty;

    [CborProperty("kind")]
    public string? Kind { get; set; }
}
