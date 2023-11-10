using Microsoft.Reactive.Testing;
using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;
using System.Reactive.Linq;

namespace SurrealDb.Net.LiveQuery.Tests;

[CollectionDefinition("Reactive")]
public class ReactiveObserveLiveQueryTests : BaseLiveQueryTests
{
    [Fact]
    public async Task ShouldObserveQuery()
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

            var cts = new CancellationTokenSource();
            cts.CancelAfter(Timeout);

            var testScheduler = new TestScheduler();

            using var _ = client
                .ObserveQuery<TestRecord>("LIVE SELECT * FROM test;")
                .SubscribeOn(testScheduler)
                .Subscribe(allResults.Add);

            await WaitLiveQueryCreationAsync(5);

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
    public async Task ShouldObserveTable()
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

            var cts = new CancellationTokenSource();
            cts.CancelAfter(Timeout);

            var testScheduler = new TestScheduler();

            using var _ = client
                .ObserveTable<TestRecord>("test")
                .SubscribeOn(testScheduler)
                .Subscribe(allResults.Add);

            await WaitLiveQueryCreationAsync(5);

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
}
