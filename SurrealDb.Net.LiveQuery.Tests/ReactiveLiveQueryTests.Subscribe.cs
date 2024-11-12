using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.LiveQuery.Tests;

public class ReactiveLiveQueryTests : BaseLiveQueryTests
{
    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldConsumeObservable(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

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

        allResults.Should().HaveCount(4);

        var firstResult = allResults[0];
        firstResult.Should().BeOfType<SurrealDbLiveQueryOpenResponse>();

        var secondResult = allResults[1];
        secondResult.Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();

        var thirdResult = allResults[2];
        thirdResult.Should().BeOfType<SurrealDbLiveQueryUpdateResponse<TestRecord>>();

        var lastResult = allResults[3];
        lastResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse<TestRecord>>();
    }

    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldConsumeObservableWithLiveQueryManuallyKilled(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

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

    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldConsumeLateObservable(string connectionString)
    {
        var allResults = new List<SurrealDbLiveQueryResponse>();

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

        allResults.Should().HaveCount(2);

        var firstResult = allResults[0];
        firstResult.Should().BeOfType<SurrealDbLiveQueryOpenResponse>();

        var lastResult = allResults[1];
        lastResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse<TestRecord>>();
    }
}
