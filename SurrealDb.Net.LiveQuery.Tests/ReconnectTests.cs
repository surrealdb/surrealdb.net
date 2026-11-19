using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Internals;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;

namespace SurrealDb.Net.LiveQuery.Tests;

public class ReconnectTests : BaseLiveQueryTests
{
    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldResubscribeLiveQueriesAfterReconnection(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = (SurrealDbClient)
                surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

            var liveTable = await client.LiveTable<TestRecord>("test");
            var liveQuery = await client.LiveQuery<TestRecord>($"LIVE SELECT * FROM test;");

            using var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveTable.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            await WaitLiveQueryCreationAsync();

            await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

            await client.Create("test", new TestRecord { Value = 1 });
            await WaitLiveQueryNotificationAsync();

            await liveTable.KillAsync();
            await WaitLiveQueryNotificationAsync();

            // 💡 Killing a resubscribed live query must target its new id on the server
            Func<Task> killResubscribedQuery = () => liveQuery.KillAsync();
            await killResubscribedQuery.Should().NotThrowAsync();

            await cts.CancelAsync();
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(3);
        allResults[0].Should().BeOfType<SurrealDbLiveQueryOpenResponse>();
        allResults[1].Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();
        allResults[2].Should().BeOfType<SurrealDbLiveQueryCloseResponse>();
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldKeepLiveQueriesWhenReconnectionIsInterrupted(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

        // 💡 Many live queries, so that the replay is still resubscribing when the connection drops again
        var liveQueries = new List<SurrealDbLiveQuery<TestRecord>>();
        for (int index = 0; index < 50; index++)
        {
            liveQueries.Add(await client.LiveTable<TestRecord>("test"));
        }
        var liveTable = liveQueries[^1];

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await foreach (var result in liveTable.WithCancellation(cts.Token))
            {
                allResults.Add(result);
            }
        });

        await WaitLiveQueryCreationAsync();

        var engine = (SurrealDbWsEngine)client.Engine;
        await engine.ForceReconnectAsync();
        await Task.Delay(10);
        await engine.ForceReconnectAsync();

        await client.Create("test", new TestRecord { Value = 1 });
        await WaitLiveQueryNotificationAsync();

        await cts.CancelAsync();

        allResults.Should().HaveCount(2);
        allResults[0].Should().BeOfType<SurrealDbLiveQueryOpenResponse>();
        allResults[1].Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldKillLiveQueryWhileItIsResubscribed(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

        var liveQueries = new List<SurrealDbLiveQuery<TestRecord>>();
        for (int index = 0; index < 50; index++)
        {
            liveQueries.Add(await client.LiveTable<TestRecord>("test"));
        }

        (await CountLiveQueriesAsync(client)).Should().Be(50);

        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        // 💡 Killed while the replay resubscribes them, one by one to avoid write conflicts on the server
        Func<Task> killAll = async () =>
        {
            foreach (var liveQuery in liveQueries)
            {
                await liveQuery.KillAsync();
            }
        };
        await killAll.Should().NotThrowAsync();

        // 💡 No subscription is left behind on the server
        (await CountLiveQueriesAsync(client))
            .Should()
            .Be(0);
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldResubscribeLiveQueriesInTheDatabaseTheyStartedIn(
        string connectionString
    )
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();
        string otherDatabase = $"{dbInfo.Database}_other";

        await using var client = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, otherDatabase);
        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

        var liveTable = await client.LiveTable<TestRecord>("test");

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await foreach (var result in liveTable.WithCancellation(cts.Token))
            {
                allResults.Add(result);
            }
        });

        await WaitLiveQueryCreationAsync();

        // 💡 The session moves on, then the connection drops
        await client.Use(dbInfo.Namespace, otherDatabase);
        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        // 💡 A request waits for the live queries to be resubscribed
        await client.RawQuery("RETURN 1;");

        await using var writer = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await writer.Use(dbInfo.Namespace, dbInfo.Database);
        await writer.Create("test", new TestRecord { Value = 1 });
        await writer.Use(dbInfo.Namespace, otherDatabase);
        await writer.Create("test", new TestRecord { Value = 2 });
        await WaitLiveQueryNotificationAsync();

        await cts.CancelAsync();

        allResults.Should().HaveCount(2);
        allResults[0].Should().BeOfType<SurrealDbLiveQueryOpenResponse>();
        allResults[1]
            .Should()
            .BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>()
            .Which.Result.Value.Should()
            .Be(1);

        // 💡 The session itself stays where it moved to
        var names = (await client.RawQuery("RETURN [session::ns(), session::db()];")).GetValue<
            List<string>
        >(0)!;
        names.Should().Equal(dbInfo.Namespace, otherDatabase);
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldEndLiveQueriesStartedAsAnotherUser(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.RawQuery(
            "DEFINE TABLE test SCHEMALESS; DEFINE USER viewer ON DATABASE PASSWORD 'secret' ROLES VIEWER;"
        );

        var liveTable = await client.LiveTable<TestRecord>("test");

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await foreach (var result in liveTable.WithCancellation(cts.Token))
            {
                allResults.Add(result);
            }
        });

        await WaitLiveQueryCreationAsync();

        // 💡 Started as root, the session is a viewer by the time the connection drops
        await client.SignIn(
            new DatabaseAuth
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Username = "viewer",
                Password = "secret",
            }
        );
        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        // 💡 A request waits for the live queries to be resubscribed
        await client.RawQuery("RETURN 1;");

        await using var writer = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await writer.Use(dbInfo.Namespace, dbInfo.Database);
        await writer.Create("test", new TestRecord { Value = 1 });
        await WaitLiveQueryNotificationAsync();

        await cts.CancelAsync();

        allResults.Should().HaveCount(2);
        allResults[0].Should().BeOfType<SurrealDbLiveQueryOpenResponse>();
        allResults[1]
            .Should()
            .BeOfType<SurrealDbLiveQueryCloseResponse>()
            .Which.Reason.Should()
            .Be(SurrealDbLiveQueryClosureReason.SocketClosed);
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    // 💡 Before 3.0, a live query does not receive notifications when it uses a query parameter
    [SinceSurrealVersion("3.0")]
    public async Task ShouldResubscribeLiveQueriesWithTheirParametersAsTheyWere(
        string connectionString
    )
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = (SurrealDbClient)surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

        var values = new List<int> { 1 };
        var liveQuery = await client.LiveRawQuery<TestRecord>(
            "LIVE SELECT * FROM test WHERE Value IN $values;",
            new Dictionary<string, object?> { { "values", values } }
        );

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await foreach (var result in liveQuery.WithCancellation(cts.Token))
            {
                allResults.Add(result);
            }
        });

        await WaitLiveQueryCreationAsync();

        // 💡 A later change to the caller's objects must not reach the server
        values.Add(2);
        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        await client.Create("test", new TestRecord { Value = 2 });
        await client.Create("test", new TestRecord { Value = 1 });
        await WaitLiveQueryNotificationAsync();

        await cts.CancelAsync();

        allResults.Should().HaveCount(2);
        allResults[1]
            .Should()
            .BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>()
            .Which.Result.Value.Should()
            .Be(1);
    }

    /// <summary>
    /// Counts the live queries on the <c>test</c> table. SurrealDB 1.x cannot use <c>INFO</c> as an
    /// expression, so this reads the whole <c>INFO FOR TABLE</c> result.
    /// </summary>
    private static async Task<int> CountLiveQueriesAsync(SurrealDbClient client)
    {
        var response = await client.RawQuery("INFO FOR TABLE test;");
        return response.GetValue<TableInfo>(0)!.Lives.Count;
    }

    private sealed class TableInfo
    {
        [CborProperty("lives")]
        public Dictionary<string, string> Lives { get; set; } = new();
    }
}
