using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.LiveQuery.Tests;

public class ReactiveOperatorsLiveQueryTests : BaseLiveQueryTests
{
    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldAggregateRecords(string connectionString)
    {
        List<TestRecord>? records = null;
        int calls = 0;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var response = await client.RawQuery("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(Timeout);

            var testScheduler = new TestScheduler();

            using var _ = liveQuery
                .GetResults()
                .ToObservable()
                .AggregateRecords(new Dictionary<string, TestRecord>())
                .Select(x => x.Values.ToList())
                .SubscribeOn(testScheduler)
                .Subscribe(results =>
                {
                    records = results;
                    calls++;
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

        calls.Should().Be(1);
    }

    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldScanRecords(string connectionString)
    {
        List<TestRecord>? records = null;
        int calls = 0;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var response = await client.RawQuery("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(Timeout);

            var testScheduler = new TestScheduler();

            using var _ = liveQuery
                .GetResults()
                .ToObservable()
                .ScanRecords(new Dictionary<string, TestRecord>())
                .Select(x => x.Values.ToList())
                .SubscribeOn(testScheduler)
                .Subscribe(results =>
                {
                    records = results;
                    calls++;
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

        calls.Should().Be(7);
    }
}
