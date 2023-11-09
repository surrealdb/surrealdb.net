using Microsoft.Reactive.Testing;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using System.Reactive.Linq;

namespace SurrealDb.Net.LiveQuery.Tests;

public class ReactiveLiveQueryTests : BaseLiveQueryTests
{
    [Fact]
    public async Task ShouldConsumeObservable()
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

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            await using var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var testScheduler = new TestScheduler();

            using var _ = liveQuery
                .ToObservable()
                .SubscribeOn(testScheduler)
                .Subscribe(allResults.Add);

            await WaitLiveQueryCreationAsync();

            testScheduler.Start();

            var record = await client.Create("test", new TestRecord { Value = 1 });
            await WaitLiveQueryNotificationAsync();

            await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
            await WaitLiveQueryNotificationAsync();

            await client.Delete(record.Id!);
            await WaitLiveQueryNotificationAsync();
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(3);

        var firstResult = allResults[0];
        firstResult.Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();

        var secondResult = allResults[1];
        secondResult.Should().BeOfType<SurrealDbLiveQueryUpdateResponse<TestRecord>>();

        var thirdResult = allResults[2];
        thirdResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse>();
    }

    [Fact]
    public async Task ShouldConsumeObservableWithLiveQueryManuallyKilled()
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

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var testScheduler = new TestScheduler();

            using var _ = liveQuery
                .ToObservable()
                .SubscribeOn(testScheduler)
                .Subscribe(allResults.Add);

            await WaitLiveQueryCreationAsync();

            testScheduler.Start();

            var record = await client.Create("test", new TestRecord { Value = 1 });
            await WaitLiveQueryNotificationAsync();

            await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
            await WaitLiveQueryNotificationAsync();

            await client.Delete(record.Id!);
            await WaitLiveQueryNotificationAsync();

            await liveQuery.KillAsync();
            await WaitLiveQueryNotificationAsync();
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

    [Fact]
    public async Task ShouldConsumeLateObservable()
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

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            await using var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var record = await client.Create("test", new TestRecord { Value = 1 });
            await WaitLiveQueryNotificationAsync();

            await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
            await WaitLiveQueryNotificationAsync();

            var testScheduler = new TestScheduler();

            using var _ = liveQuery
                .ToObservable()
                .SubscribeOn(testScheduler)
                .Subscribe(allResults.Add);

            testScheduler.Start();

            await client.Delete(record.Id!);
            await WaitLiveQueryNotificationAsync();
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(1);

        var firstResult = allResults[0];
        firstResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse>();
    }

    [Fact]
    public async Task ShouldConsumeAggregatedData()
    {
        const string url = "ws://localhost:8000/rpc";

        List<TestRecord>? records = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var testScheduler = new TestScheduler();

            using var _ = liveQuery
                .GetResults()
                .ToObservable()
                .Aggregate(
                    new Dictionary<string, TestRecord>(), // Start from an empty list or from the current list of records
                    (acc, response) =>
                    {
                        if (
                            response is SurrealDbLiveQueryCreateResponse<TestRecord> createResponse
                            && createResponse.Result.Id is not null
                        )
                        {
                            acc[createResponse.Result.Id.ToString()] = createResponse.Result;
                        }
                        if (
                            response is SurrealDbLiveQueryUpdateResponse<TestRecord> updateResponse
                            && updateResponse.Result.Id is not null
                        )
                        {
                            acc[updateResponse.Result.Id.ToString()] = updateResponse.Result;
                        }
                        if (
                            response is SurrealDbLiveQueryDeleteResponse deleteResponse
                            && deleteResponse.Result is not null
                        )
                        {
                            acc.Remove(deleteResponse.Result.ToString());
                        }

                        return acc;
                    }
                )
                .Select(x => x.Values.ToList())
                .SubscribeOn(testScheduler)
                .Subscribe(results =>
                {
                    records = results;
                });

            await WaitLiveQueryCreationAsync();

            testScheduler.Start();

            var record1 = await client.Create("test", new TestRecord { Value = 1 });
            await WaitLiveQueryNotificationAsync();

            await client.Upsert(new TestRecord { Id = record1.Id, Value = 2 });
            await WaitLiveQueryNotificationAsync();

            var record2 = await client.Create("test", new TestRecord { Value = 44 });
            await WaitLiveQueryNotificationAsync();

            var record3 = await client.Create("test", new TestRecord { Value = 66 });
            await WaitLiveQueryNotificationAsync();

            var record4 = await client.Create("test", new TestRecord { Value = 8 });
            await WaitLiveQueryNotificationAsync();

            await client.Upsert(new TestRecord { Id = record1.Id, Value = 2 });
            await WaitLiveQueryNotificationAsync();

            await client.Upsert(new TestRecord { Id = record2.Id, Value = 45 });
            await WaitLiveQueryNotificationAsync();

            await client.Delete(record1.Id!);
            await WaitLiveQueryNotificationAsync();

            await liveQuery.DisposeAsync();
            await WaitLiveQueryNotificationAsync();
        };

        await func.Should().NotThrowAsync();

        var expected = new List<TestRecord>
        {
            new() { Id = records![0].Id, Value = 45 },
            new() { Id = records![1].Id, Value = 66 },
            new() { Id = records![2].Id, Value = 8 },
        };

        records.Should().BeEquivalentTo(expected);
    }
}
