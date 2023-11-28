using Microsoft.Reactive.Testing;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using System.Reactive.Linq;

namespace SurrealDb.Net.LiveQuery.Tests;

[CollectionDefinition("Reactive")]
public class ReactiveLiveQueryTests : BaseLiveQueryTests
{
    [Fact]
    public async Task ShouldConsumeObservable()
    {
        const string url = "ws://127.0.0.1:8000/rpc";

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
            cts.CancelAfter(Timeout);

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
        const string url = "ws://127.0.0.1:8000/rpc";

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
            cts.CancelAfter(Timeout);

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
        const string url = "ws://127.0.0.1:8000/rpc";

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
            cts.CancelAfter(Timeout);

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
}
