using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;

namespace SurrealDb.Net.LiveQuery.Tests;

public class LiveQueryTests : BaseLiveQueryTests
{
    [Test]
    public async Task ShouldNotBeSupportedOnHttpProtocol()
    {
        const string connectionString = "Endpoint=http://127.0.0.1:8000";

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

            await using var liveQuery = await client.LiveQuery<int>($"LIVE SELECT * FROM test;");
        };

        await func.Should().ThrowAsync<NotSupportedException>();
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldReceiveData(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

            var liveQuery = await client.LiveQuery<TestRecord>($"LIVE SELECT * FROM test;");

            using var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await WaitLiveQueryCreationAsync();

                var record = await client.Create("test", new TestRecord { Value = 1 });
                await WaitLiveQueryNotificationAsync();

                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
                await WaitLiveQueryNotificationAsync();

                await client.Delete(record.Id!);
                await WaitLiveQueryNotificationAsync();

                await liveQuery.KillAsync();
                await WaitLiveQueryNotificationAsync();

                await cts.CancelAsync();
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                await cts.CancelAsync();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(5);

        var firstResult = allResults[0];
        firstResult.Should().BeOfType<SurrealDbLiveQueryOpenResponse>();

        var secondResult = allResults[1];
        secondResult.Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();

        var thirdResult = allResults[2];
        thirdResult.Should().BeOfType<SurrealDbLiveQueryUpdateResponse<TestRecord>>();

        var fourthResult = allResults[3];
        fourthResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse<TestRecord>>();

        var lastResult = allResults[4];
        lastResult.Should().BeOfType<SurrealDbLiveQueryCloseResponse>();
    }
}
