using SurrealDb.Net.Internals.Errors;

namespace SurrealDb.Net.Internals.Http;

internal sealed class SurrealDbHttpErrorResponse : ISurrealDbHttpResponse
{
    public RpcErrorResponseContent Error { get; set; } = new();
}
