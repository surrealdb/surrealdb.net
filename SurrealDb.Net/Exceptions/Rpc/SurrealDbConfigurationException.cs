namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: feature or configuration not supported (live queries, GraphQL).
/// </summary>
public sealed class SurrealDbConfigurationException : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// True if live queries are not supported by the server configuration.
    /// </summary>
    public bool IsLiveQueryNotSupported => Kind == "LiveQueryNotSupported";

    internal SurrealDbConfigurationException(string message, string? kind)
        : base(message)
    {
        Kind = kind;
    }
}
