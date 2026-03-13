namespace SurrealDb.Net.Internals.Errors;

internal sealed class RpcErrorDetails
{
    public string Kind { get; internal set; } = string.Empty;

    /// <summary>
    /// Kind-specific structured details (wire format: { "kind": "...", "details": ... } or flat structs).
    /// Null when the server sends no details (e.g. unit variants).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Details { get; internal set; }
}
