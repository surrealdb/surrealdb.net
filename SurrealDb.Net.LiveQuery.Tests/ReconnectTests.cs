using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Internals;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
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

            await using var client = surrealDbClientGenerator.Create(connectionString);
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

        await using var client = surrealDbClientGenerator.Create(connectionString);
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

        await using var client = surrealDbClientGenerator.Create(connectionString);
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
