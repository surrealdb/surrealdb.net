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
    /// Changes waiting to be published, in the order they were made.
    /// </summary>
    private readonly Queue<ConnectionStatusChangedEventArgs> _pendingChanges = new();
    private bool _isPublishing;

    /// <summary>
    /// Changes the status, and returns the previous one.
    /// </summary>
    public ConnectionStatus Set(ConnectionStatus status, Exception? exception = null)
    {
        return SetIf(null, status, exception);
    }

    /// <summary>
    /// Changes the status if it is <paramref name="expectedStatus"/>, and returns the previous one.
    /// </summary>
    public ConnectionStatus SetIf(
        ConnectionStatus? expectedStatus,
        ConnectionStatus status,
        Exception? exception = null
    )
    {
        ConnectionStatus previousStatus;
        lock (_lock)
        {
            previousStatus = _status;
            if (
                previousStatus == status
                || (expectedStatus.HasValue && previousStatus != expectedStatus.Value)
            )
            {
                return previousStatus;
            }

            _status = status;
            _pendingChanges.Enqueue(
                new ConnectionStatusChangedEventArgs(status, previousStatus, exception)
            );

            // 💡 One thread publishes at a time, so that handlers see changes in the order they were made
            if (_isPublishing)
            {
                return previousStatus;
            }
            _isPublishing = true;
        }

        PublishPendingChanges();

        return previousStatus;
    }

    private void PublishPendingChanges()
    {
        while (true)
        {
            ConnectionStatusChangedEventArgs change;
            lock (_lock)
            {
                if (_pendingChanges.Count == 0)
                {
                    _isPublishing = false;
                    return;
                }
                change = _pendingChanges.Dequeue();
            }

            var handlers = StatusChanged;
            if (handlers is null)
            {
                continue;
            }

            foreach (
                EventHandler<ConnectionStatusChangedEventArgs> handler in handlers.GetInvocationList()
            )
            {
                try
                {
                    handler(_sender, change);
                }
                catch
                {
                    // 💡 A failing handler must neither break the connection nor the other handlers
                }
            }
        }
    }
}
