namespace SurrealDb.Net.Models;

/// <summary>
/// The state of the connection to a SurrealDB instance.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Not connected. Either the client never connected, it was disposed,
    /// or it stopped trying to reconnect after the connection was lost.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The client is connecting to the server.
    /// </summary>
    Connecting,

    /// <summary>
    /// The connection was lost and the client is trying to connect again.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The client is connected to the server.
    /// </summary>
    Connected,
}
