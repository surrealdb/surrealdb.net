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
}
