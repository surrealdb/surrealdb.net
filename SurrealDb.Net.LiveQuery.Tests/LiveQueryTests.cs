﻿using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;

namespace SurrealDb.Net.LiveQuery.Tests;

[CollectionDefinition("LiveQuery")]
public class LiveQueryTests : BaseLiveQueryTests
{
    [Fact]
    public async Task ShouldNotBeSupportedOnHttpProtocol()
    {
        const string url = "http://localhost:8000";

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await using var liveQuery = await client.LiveQuery<int>("LIVE SELECT * FROM test;");
        };

        await func.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task ShouldReceiveData()
    {
        const string url = "ws://localhost:8000/rpc";

        var allResults = new List<SurrealDbLiveQueryResponse>();

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var liveQuery = await client.LiveQuery<TestRecord>("LIVE SELECT * FROM test;");

            var cts = new CancellationTokenSource();

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

                cts.Cancel();
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(4);

        var firstResult = allResults[0];
        firstResult.Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();

        var secondResult = allResults[1];
        secondResult.Should().BeOfType<SurrealDbLiveQueryUpdateResponse<TestRecord>>();

        var thirdResult = allResults[2];
        thirdResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse>();

        var lastResult = allResults[3];
        lastResult.Should().BeOfType<SurrealDbLiveQueryCloseResponse>();
    }
}