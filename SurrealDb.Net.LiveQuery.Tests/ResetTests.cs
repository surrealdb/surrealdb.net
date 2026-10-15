using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;

namespace SurrealDb.Net.LiveQuery.Tests;

public class ResetTests : BaseLiveQueryTests
{
    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCloseLiveQueriesOnReset(string connectionString)
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

            using var cts = new CancellationTokenSource();

            var readTask = Task.Run(async () =>
            {
                await foreach (var result in liveTable.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            await WaitLiveQueryCreationAsync();

            await client.Reset();

            // 💡 The channel completes once the live query is closed
            await readTask.WaitAsync(TimeSpan.FromSeconds(5));
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(2);
        allResults[0].Should().BeOfType<SurrealDbLiveQueryOpenResponse>();
        allResults[1]
            .Should()
            .BeOfType<SurrealDbLiveQueryCloseResponse>()
            .Which.Reason.Should()
            .Be(SurrealDbLiveQueryClosureReason.QueryKilled);
    }
}
