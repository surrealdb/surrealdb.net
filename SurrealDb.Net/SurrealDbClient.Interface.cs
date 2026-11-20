using SurrealDb.Net.Models;

namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public interface ISurrealDbClient
    : ISurrealDbSharedSession,
        ISurrealDbSharedMethods,
        IAsyncDisposable
{
    /// <summary>
    /// The current state of the connection.
    /// </summary>
    ConnectionStatus Status { get; }

    /// <summary>
    /// Raised when the state of the connection changes,
    /// e.g. when a WebSocket connection is lost and the client starts to reconnect.
    /// Handlers run on the connection's thread and should return quickly.
    /// </summary>
    event EventHandler<ConnectionStatusChangedEventArgs>? StatusChanged;
}
