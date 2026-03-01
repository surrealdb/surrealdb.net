using SurrealDb.Net.Internals.Errors;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsErrorResponse : ISurrealDbWsStandardResponse
{
    public string Id { get; set; } = string.Empty;
    public RpcErrorResponseContent Error { get; set; } = new();
}
