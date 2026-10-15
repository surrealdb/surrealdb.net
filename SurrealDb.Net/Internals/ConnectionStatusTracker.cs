using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals;

/// <summary>
/// Holds the <see cref="ConnectionStatus"/> of an engine and raises an event when it changes.
/// </summary>
internal sealed class ConnectionStatusTracker
{
    private readonly object _sender;
    private readonly object _lock = new();
    private ConnectionStatus _status = ConnectionStatus.Disconnected;

    public ConnectionStatus Status
    {
        get
        {
            lock (_lock)
            {
                return _status;
            }
        }
    }

    public event EventHandler<ConnectionStatusChangedEventArgs>? StatusChanged;

    public ConnectionStatusTracker(object sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Changes the status, and returns the previous one.
    /// </summary>
    public ConnectionStatus Set(ConnectionStatus status, Exception? exception = null)
    {
        ConnectionStatus previousStatus;
        lock (_lock)
        {
            previousStatus = _status;
            _status = status;
        }

        if (previousStatus != status)
        {
            try
            {
                StatusChanged?.Invoke(
                    _sender,
                    new ConnectionStatusChangedEventArgs(status, previousStatus, exception)
                );
            }
            catch
            {
                // 💡 A failing handler must not break the connection
            }
        }

        return previousStatus;
    }
}
