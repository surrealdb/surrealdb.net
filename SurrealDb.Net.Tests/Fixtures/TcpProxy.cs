using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SurrealDb.Net.Tests.Fixtures;

/// <summary>
/// Forwards TCP connections to a local port, so that tests can cut the connection to a SurrealDB instance.
/// </summary>
public sealed class TcpProxy : IAsyncDisposable
{
    private readonly int _targetPort;
    private readonly ConcurrentDictionary<TcpClient, byte> _connections = new();
    private TcpListener? _listener;
    private CancellationTokenSource _cancellation = new();

    public int Port { get; private set; }

    public TcpProxy(int targetPort)
    {
        _targetPort = targetPort;
    }

    /// <summary>
    /// Accepts connections, on the same port as before when restarted.
    /// </summary>
    public void Start()
    {
        _cancellation = new CancellationTokenSource();

        var listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Server.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true
        );
        listener.Start();

        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _listener = listener;

        _ = AcceptAsync(listener, _cancellation.Token);
    }

    /// <summary>
    /// Cuts the open connections, new connections are still accepted.
    /// </summary>
    public void DropConnections()
    {
        foreach (var connection in _connections.Keys)
        {
            Close(connection);
        }
    }

    /// <summary>
    /// Cuts the open connections and refuses new ones.
    /// </summary>
    public void Stop()
    {
        _cancellation.Cancel();
        _listener?.Stop();
        _listener = null;
        DropConnections();
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return default;
    }

    private async Task AcceptAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch
            {
                return;
            }

            var upstream = new TcpClient();
            try
            {
                await upstream.ConnectAsync(IPAddress.Loopback, _targetPort, cancellationToken);
            }
            catch
            {
                Close(client);
                upstream.Dispose();
                continue;
            }

            _connections.TryAdd(client, 0);
            _connections.TryAdd(upstream, 0);

            _ = PipeAsync(client, upstream);
            _ = PipeAsync(upstream, client);
        }
    }

    private async Task PipeAsync(TcpClient from, TcpClient to)
    {
        try
        {
            await from.GetStream().CopyToAsync(to.GetStream());
        }
        catch
        {
            // 💡 The connection was cut
        }
        finally
        {
            Close(from);
            Close(to);
        }
    }

    private void Close(TcpClient connection)
    {
        _connections.TryRemove(connection, out _);
        try
        {
            // 💡 Reset the connection instead of closing it gracefully
            connection.LingerState = new LingerOption(true, 0);
        }
        catch
        {
            // 💡 Already closed
        }
        connection.Dispose();
    }
}
