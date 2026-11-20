namespace SurrealDb.Net.Models;

/// <summary>
/// Describes a change of <see cref="ConnectionStatus"/>.
/// </summary>
public sealed class ConnectionStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// The new status of the connection.
    /// </summary>
    public ConnectionStatus Status { get; }

    /// <summary>
    /// The status of the connection before this change.
    /// </summary>
    public ConnectionStatus PreviousStatus { get; }

    /// <summary>
    /// The error that caused the change, if any.
    /// </summary>
    public Exception? Exception { get; }

    public ConnectionStatusChangedEventArgs(
        ConnectionStatus status,
        ConnectionStatus previousStatus,
        Exception? exception = null
    )
    {
        Status = status;
        PreviousStatus = previousStatus;
        Exception = exception;
    }
}
