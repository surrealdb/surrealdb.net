using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Exceptions.Rpc;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests;

public class ConnectionStatusTests
{
    private static (SurrealDbOptions Options, TcpProxy Proxy) CreateProxiedOptions(
        string connectionString,
        Action<ReconnectOptions>? configureReconnect = null
    )
    {
        var options = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .WithReconnect(configureReconnect ?? (_ => { }))
            .Build();

        var endpoint = new Uri(options.Endpoint!);
        var proxy = new TcpProxy(endpoint.Port);
        proxy.Start();

        options.Endpoint = new UriBuilder(endpoint) { Port = proxy.Port }.Uri.ToString();

        return (options, proxy);
    }

    private static List<ConnectionStatusChangedEventArgs> RecordStatusChanges(
        SurrealDbClient client
    )
    {
        var changes = new List<ConnectionStatusChangedEventArgs>();
        client.StatusChanged += (_, e) =>
        {
            lock (changes)
            {
                changes.Add(e);
            }
        };
        return changes;
    }

    private static ConnectionStatus[] Statuses(List<ConnectionStatusChangedEventArgs> changes)
    {
        lock (changes)
        {
            return changes.Select(c => c.Status).ToArray();
        }
    }

    private static async Task WaitForStatusAsync(SurrealDbClient client, ConnectionStatus status)
    {
        var stopwatch = Stopwatch.StartNew();
        while (client.Status != status)
        {
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(15))
            {
                throw new TimeoutException(
                    $"Expected status {status}, but it is still {client.Status}."
                );
            }
            await Task.Delay(20);
        }
    }

    [Test]
    public void ShouldIncreaseDelayAfterEachFailedAttempt()
    {
        var reconnect = new ReconnectOptions
        {
            RetryDelay = TimeSpan.FromSeconds(1),
            RetryDelayMax = TimeSpan.FromSeconds(10),
            RetryDelayMultiplier = 2,
            RetryDelayJitter = 0,
        };

        reconnect.GetDelay(1, 0.5).Should().Be(TimeSpan.FromSeconds(1));
        reconnect.GetDelay(2, 0.5).Should().Be(TimeSpan.FromSeconds(2));
        reconnect.GetDelay(3, 0.5).Should().Be(TimeSpan.FromSeconds(4));
        reconnect.GetDelay(4, 0.5).Should().Be(TimeSpan.FromSeconds(8));
        reconnect.GetDelay(5, 0.5).Should().Be(TimeSpan.FromSeconds(10));
    }

    [Test]
    public void ShouldApplyJitterToDelay()
    {
        var reconnect = new ReconnectOptions
        {
            RetryDelay = TimeSpan.FromSeconds(1),
            RetryDelayJitter = 0.1,
        };

        reconnect.GetDelay(1, 0).Should().Be(TimeSpan.FromMilliseconds(900));
        reconnect.GetDelay(1, 0.5).Should().Be(TimeSpan.FromSeconds(1));
        reconnect.GetDelay(1, 1).Should().Be(TimeSpan.FromMilliseconds(1100));
    }

    [Test]
    public void ShouldLimitAttempts()
    {
        new ReconnectOptions().AllowsAttempt(1_000).Should().BeTrue();

        var reconnect = new ReconnectOptions { Attempts = 3 };
        reconnect.AllowsAttempt(2).Should().BeTrue();
        reconnect.AllowsAttempt(3).Should().BeFalse();

        new ReconnectOptions { Enabled = false }
            .AllowsAttempt(0)
            .Should()
            .BeFalse();
    }

    [Test]
    public void ShouldRejectInvalidReconnectOptions()
    {
        var act = () =>
            SurrealDbOptions.Create().WithReconnect(r => r.RetryDelayMultiplier = 0.5).Build();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldReportStatusWhileReconnecting(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        var (options, proxy) = CreateProxiedOptions(connectionString);
        await using var _ = proxy;

        await using var client = new SurrealDbClient(options);
        var changes = RecordStatusChanges(client);

        client.Status.Should().Be(ConnectionStatus.Disconnected);

        await client.Use(dbInfo.Namespace, dbInfo.Database);
        client.Status.Should().Be(ConnectionStatus.Connected);

        proxy.DropConnections();

        // 💡 A local reconnection can be over before the status is polled, so wait for the events
        var stopwatch = Stopwatch.StartNew();
        while (Statuses(changes).Length < 4)
        {
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(15))
            {
                throw new TimeoutException(
                    $"Expected 4 status changes, got {string.Join(", ", Statuses(changes))}."
                );
            }
            await Task.Delay(20);
        }

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string[]>(0).Should().Equal(dbInfo.Namespace, dbInfo.Database);

        Statuses(changes)
            .Should()
            .Equal(
                ConnectionStatus.Connecting,
                ConnectionStatus.Connected,
                ConnectionStatus.Reconnecting,
                ConnectionStatus.Connected
            );
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldBackOffThenGiveUp(string connectionString)
    {
        var (options, proxy) = CreateProxiedOptions(
            connectionString,
            reconnect =>
            {
                reconnect.Attempts = 3;
                reconnect.RetryDelay = TimeSpan.FromMilliseconds(300);
                reconnect.RetryDelayJitter = 0;
            }
        );
        await using var _ = proxy;

        await using var client = new SurrealDbClient(options);
        await client.Connect();

        proxy.Stop();
        await WaitForStatusAsync(client, ConnectionStatus.Reconnecting);
        var stopwatch = Stopwatch.StartNew();

        await WaitForStatusAsync(client, ConnectionStatus.Disconnected);

        // 💡 300 ms after the 1st failed attempt, 600 ms after the 2nd, then the 3rd fails
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(850));
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldConnectAgainOnNextRequestAfterGivingUp(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        var (options, proxy) = CreateProxiedOptions(
            connectionString,
            reconnect =>
            {
                reconnect.Attempts = 1;
                reconnect.RetryDelay = TimeSpan.FromMilliseconds(100);
            }
        );
        await using var _ = proxy;

        await using var client = new SurrealDbClient(options);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.Set("answer", 42);

        proxy.Stop();
        await WaitForStatusAsync(client, ConnectionStatus.Disconnected);

        Func<Task> whileServerIsDown = () => client.RawQuery("RETURN 1;");
        await whileServerIsDown.Should().ThrowAsync<SurrealDbConnectionException>();
        client.Status.Should().Be(ConnectionStatus.Disconnected);

        proxy.Start();

        var response = await client.RawQuery(
            "RETURN [session::ns(), session::db(), <string> $answer];"
        );
        response.GetValue<string[]>(0).Should().Equal(dbInfo.Namespace, dbInfo.Database, "42");
        client.Status.Should().Be(ConnectionStatus.Connected);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldNotReconnectWhenDisabled(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        var (options, proxy) = CreateProxiedOptions(
            connectionString,
            reconnect => reconnect.Enabled = false
        );
        await using var _ = proxy;

        await using var client = new SurrealDbClient(options);
        var changes = RecordStatusChanges(client);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        proxy.DropConnections();
        await WaitForStatusAsync(client, ConnectionStatus.Disconnected);

        Statuses(changes)
            .Should()
            .Equal(
                ConnectionStatus.Connecting,
                ConnectionStatus.Connected,
                ConnectionStatus.Disconnected
            );

        // 💡 The next request connects again
        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string[]>(0).Should().Equal(dbInfo.Namespace, dbInfo.Database);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldBeDisconnectedOnceDisposed(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var client = surrealDbClientGenerator.Create(connectionString);

        await client.Connect();
        client.Status.Should().Be(ConnectionStatus.Connected);

        await client.DisposeAsync();
        client.Status.Should().Be(ConnectionStatus.Disconnected);
    }

    [Test]
    public void ShouldNotifyEveryHandlerWhenOneThrows()
    {
        var tracker = new ConnectionStatusTracker(this);
        var received = new List<ConnectionStatus>();
        tracker.StatusChanged += (_, _) => throw new InvalidOperationException();
        tracker.StatusChanged += (_, e) => received.Add(e.Status);

        tracker.Set(ConnectionStatus.Connecting);
        tracker.Set(ConnectionStatus.Connected);

        received.Should().Equal(ConnectionStatus.Connecting, ConnectionStatus.Connected);
    }

    [Test]
    public async Task ShouldNotifyChangesInTheOrderTheyWereMade()
    {
        var tracker = new ConnectionStatusTracker(this);
        var changes = new List<ConnectionStatusChangedEventArgs>();
        tracker.StatusChanged += (_, e) =>
        {
            lock (changes)
            {
                changes.Add(e);
            }
            Thread.SpinWait(100);
        };

        await Task.WhenAll(
            Enumerable
                .Range(0, 8)
                .Select(thread =>
                    Task.Run(() =>
                    {
                        for (int index = 0; index < 500; index++)
                        {
                            tracker.Set(
                                (index + thread) % 2 == 0
                                    ? ConnectionStatus.Connected
                                    : ConnectionStatus.Disconnected
                            );
                        }
                    })
                )
        );

        // 💡 Each change starts from the status the previous one ended on
        for (int index = 1; index < changes.Count; index++)
        {
            changes[index].PreviousStatus.Should().Be(changes[index - 1].Status);
        }
        changes[^1].Status.Should().Be(tracker.Status);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldNotReconnectWhenNoAttemptIsAllowed(string connectionString)
    {
        var (options, proxy) = CreateProxiedOptions(
            connectionString,
            reconnect => reconnect.Attempts = 0
        );
        await using var _ = proxy;

        await using var client = new SurrealDbClient(options);
        var changes = RecordStatusChanges(client);
        await client.Connect();

        proxy.DropConnections();
        await WaitForStatusAsync(client, ConnectionStatus.Disconnected);

        Statuses(changes)
            .Should()
            .Equal(
                ConnectionStatus.Connecting,
                ConnectionStatus.Connected,
                ConnectionStatus.Disconnected
            );
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldReportHttpStatusFromRequestOutcomes(string connectionString)
    {
        if (!connectionString.Contains("http://"))
            return;

        var (options, proxy) = CreateProxiedOptions(connectionString);
        await using var _ = proxy;

        await using var client = new SurrealDbClient(options);
        await client.Connect();
        client.Status.Should().Be(ConnectionStatus.Connected);

        proxy.Stop();

        (await client.Health()).Should().BeFalse();
        client.Status.Should().Be(ConnectionStatus.Disconnected);

        proxy.Start();

        // 💡 A successful request means the server can be reached again
        await client.RawQuery("RETURN 1;");
        client.Status.Should().Be(ConnectionStatus.Connected);

        proxy.Stop();

        Func<Task> whileServerIsDown = () => client.RawQuery("RETURN 1;");
        await whileServerIsDown.Should().ThrowAsync<HttpRequestException>();
        client.Status.Should().Be(ConnectionStatus.Disconnected);

        proxy.Start();

        await client.RawQuery("RETURN 1;");
        client.Status.Should().Be(ConnectionStatus.Connected);
    }
}
