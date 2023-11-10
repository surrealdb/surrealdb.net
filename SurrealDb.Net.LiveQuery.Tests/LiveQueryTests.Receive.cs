using SurrealDb.Net.LiveQuery.Tests.Abstract;
using SurrealDb.Net.LiveQuery.Tests.Models;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.LiveQuery.Tests;

[CollectionDefinition("LiveQuery")]
public class ReceiveLiveQueryTests : BaseLiveQueryTests
{
    [Fact]
    public async Task ShouldLiveQueryReceiveCreatedRecord()
    {
        const string url = "ws://localhost:8000/rpc";

        var results = new List<SurrealDbLiveQueryResponse>();

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

            _ = Task.Run(async () =>
            {
                await WaitLiveQueryCreationAsync();

                await client.Create("test", new TestRecord { Value = 1 });
                await WaitLiveQueryNotificationAsync();

                cts.Cancel();
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    results.Add(result);
                }
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        results.Should().HaveCount(1);

        var firstResult = results.First();

        firstResult.Should().BeOfType<SurrealDbLiveQueryCreateResponse<TestRecord>>();

        var firstResultAsSurrealDbLiveQueryResultResponse =
            (SurrealDbLiveQueryCreateResponse<TestRecord>)firstResult;

        firstResultAsSurrealDbLiveQueryResultResponse.Result.Value.Should().Be(1);
    }

    [Fact]
    public async Task ShouldLiveQueryReceiveUpdatedRecord()
    {
        const string url = "ws://localhost:8000/rpc";

        var results = new List<SurrealDbLiveQueryResponse>();

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var record = await client.Create("test", new TestRecord { Value = 1 });

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            await using var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await WaitLiveQueryCreationAsync();

                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
                await WaitLiveQueryNotificationAsync();

                cts.Cancel();
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    results.Add(result);
                }
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        results.Should().HaveCount(1);

        var firstResult = results.First();

        firstResult.Should().BeOfType<SurrealDbLiveQueryUpdateResponse<TestRecord>>();

        var firstResultAsSurrealDbLiveQueryResultResponse =
            (SurrealDbLiveQueryUpdateResponse<TestRecord>)firstResult;

        firstResultAsSurrealDbLiveQueryResultResponse.Result.Value.Should().Be(2);
    }

    [Fact]
    public async Task ShouldLiveQueryReceiveDeletedRecord()
    {
        const string url = "ws://localhost:8000/rpc";

        var results = new List<SurrealDbLiveQueryResponse>();

        TestRecord? record = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            record = await client.Create("test", new TestRecord { Value = 1 });

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            await using var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await WaitLiveQueryCreationAsync();

                await client.Delete(record.Id!);
                await WaitLiveQueryNotificationAsync();

                cts.Cancel();
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    results.Add(result);
                }
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        results.Should().HaveCount(1);

        var firstResult = results.First();

        firstResult.Should().BeOfType<SurrealDbLiveQueryDeleteResponse>();

        var firstResultAsSurrealDbLiveQueryResultResponse =
            (SurrealDbLiveQueryDeleteResponse)firstResult;

        firstResultAsSurrealDbLiveQueryResultResponse.Result.Should().Be(record!.Id);
    }

    [Fact]
    public async Task ShouldLiveQueryReceiveSocketClosed()
    {
        const string url = "ws://localhost:8000/rpc";

        var results = new List<SurrealDbLiveQueryResponse>();

        TestRecord? record = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            record = await client.Create("test", new TestRecord { Value = 1 });

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await WaitLiveQueryCreationAsync();

                client.Dispose();
                await WaitLiveQueryNotificationAsync();

                cts.Cancel();
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    results.Add(result);
                }
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        results.Should().HaveCount(1);

        var firstResult = results.First();

        firstResult.Should().BeOfType<SurrealDbLiveQueryCloseResponse>();

        var firstResultAsSurrealDbLiveQueryCloseResponse =
            (SurrealDbLiveQueryCloseResponse)firstResult;

        firstResultAsSurrealDbLiveQueryCloseResponse.Reason
            .Should()
            .Be(SurrealDbLiveQueryClosureReason.SocketClosed);
    }

    [Fact]
    public async Task ShouldLiveQueryReceiveQueryKilled()
    {
        const string url = "ws://localhost:8000/rpc";

        var results = new List<SurrealDbLiveQueryResponse>();

        TestRecord? record = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            record = await client.Create("test", new TestRecord { Value = 1 });

            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            var liveQuery = client.ListenLive<TestRecord>(liveQueryUuid);

            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await WaitLiveQueryCreationAsync();

                await liveQuery.KillAsync();
                await WaitLiveQueryNotificationAsync();

                cts.Cancel();
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    results.Add(result);
                }
            });

            await Task.Delay(Timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        results.Should().HaveCount(1);

        var firstResult = results.First();

        firstResult.Should().BeOfType<SurrealDbLiveQueryCloseResponse>();

        var firstResultAsSurrealDbLiveQueryCloseResponse =
            (SurrealDbLiveQueryCloseResponse)firstResult;

        firstResultAsSurrealDbLiveQueryCloseResponse.Reason
            .Should()
            .Be(SurrealDbLiveQueryClosureReason.QueryKilled);
    }
}
