using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using Record = SurrealDb.Net.Models.Record;

namespace SurrealDb.Net.Tests;

public class TestRecord : Record
{
    public int Value { get; set; }
}

public class ListenLiveTests
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

            var liveQueryUuid = Guid.NewGuid();

            await using var liveQuery = client.ListenLive<int>(liveQueryUuid);
        };

        await func.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task ShouldAutomaticallyKillLiveQueryWhenDisposedOnWsProtocol()
    {
        const string url = "ws://localhost:8000/rpc";

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        using var client = surrealDbClientGenerator.Create(url);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        Guid liveQueryUuid = Guid.Empty;

        Func<Task> createLiveQueryFunc = async () =>
        {
            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            liveQueryUuid = okResult.GetValue<Guid>();

            await using var liveQuery = client.ListenLive<int>(liveQueryUuid);
        };

        Func<Task> liveQueryAlreadyKilledFunc = async () =>
        {
            await client.Kill(liveQueryUuid);
        };

        liveQueryUuid.Should().BeEmpty();

        await createLiveQueryFunc.Should().NotThrowAsync();

        liveQueryUuid.Should().NotBeEmpty();

        await liveQueryAlreadyKilledFunc
            .Should()
            .ThrowAsync<SurrealDbException>()
            .WithMessage(
                "There was a problem with the database: Can not execute KILL statement using id '$id'"
            );
    }

    [Fact]
    public async Task ShouldManuallyKillLiveQueryOnWsProtocol()
    {
        const string url = "ws://localhost:8000/rpc";

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        using var client = surrealDbClientGenerator.Create(url);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        SurrealDbLiveQuery<int>? liveQuery = null;

        Func<Task> createLiveQueryFunc = async () =>
        {
            var response = await client.Query("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            var liveQueryUuid = okResult.GetValue<Guid>();

            liveQuery = client.ListenLive<int>(liveQueryUuid);
        };

        Func<Task> manuallyKillLiveQueryFunc = async () =>
        {
            await liveQuery!.KillAsync();
        };

        Func<Task> liveQueryAlreadyKilledFunc = async () =>
        {
            await liveQuery!.KillAsync();
        };

        await createLiveQueryFunc.Should().NotThrowAsync();

        await manuallyKillLiveQueryFunc.Should().NotThrowAsync();

        await liveQueryAlreadyKilledFunc
            .Should()
            .ThrowAsync<SurrealDbException>()
            .WithMessage(
                "There was a problem with the database: Can not execute KILL statement using id '$id'"
            );
    }

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

            bool hasReceivedFirstResult = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);

                await client.Create("test", new TestRecord { Value = 1 });

                while (!hasReceivedFirstResult)
                {
                    await Task.Delay(10);
                }

                cts.Cancel();
            });

            await foreach (var result in liveQuery.WithCancellation(cts.Token))
            {
                results.Add(result);
                hasReceivedFirstResult = true;
            }
        };

        await func.Should().ThrowAsync<OperationCanceledException>();

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

            bool hasReceivedFirstResult = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);

                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });

                while (!hasReceivedFirstResult)
                {
                    await Task.Delay(10);
                }

                cts.Cancel();
            });

            await foreach (var result in liveQuery.WithCancellation(cts.Token))
            {
                results.Add(result);
                hasReceivedFirstResult = true;
            }
        };

        await func.Should().ThrowAsync<OperationCanceledException>();

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

            bool hasReceivedFirstResult = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);

                await client.Delete(record.Id!);

                while (!hasReceivedFirstResult)
                {
                    await Task.Delay(10);
                }

                cts.Cancel();
            });

            await foreach (var result in liveQuery.WithCancellation(cts.Token))
            {
                results.Add(result);
                hasReceivedFirstResult = true;
            }
        };

        await func.Should().ThrowAsync<OperationCanceledException>();

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

            bool hasReceivedFirstResult = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);

                client.Dispose();

                while (!hasReceivedFirstResult)
                {
                    await Task.Delay(10);
                }

                cts.Cancel();
            });

            await foreach (var result in liveQuery.WithCancellation(cts.Token))
            {
                results.Add(result);
                hasReceivedFirstResult = true;
            }
        };

        await func.Should().ThrowAsync<OperationCanceledException>();

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

            bool hasReceivedFirstResult = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);

                await liveQuery.KillAsync();

                while (!hasReceivedFirstResult)
                {
                    await Task.Delay(10);
                }

                cts.Cancel();
            });

            await foreach (var result in liveQuery.WithCancellation(cts.Token))
            {
                results.Add(result);
                hasReceivedFirstResult = true;
            }
        };

        await func.Should().ThrowAsync<OperationCanceledException>();

        results.Should().HaveCount(1);

        var firstResult = results.First();

        firstResult.Should().BeOfType<SurrealDbLiveQueryCloseResponse>();

        var firstResultAsSurrealDbLiveQueryCloseResponse =
            (SurrealDbLiveQueryCloseResponse)firstResult;

        firstResultAsSurrealDbLiveQueryCloseResponse.Reason
            .Should()
            .Be(SurrealDbLiveQueryClosureReason.QueryKilled);
    }

    // TODO : Function .GetResults() to exclude Close result and only return action results
    // TODO : Function .GetCreatedRecords() to only return CREATE action results
    // TODO : Function .GetUpdatedRecords() to only return UPDATE action results
    // TODO : Function .GetDeletedIds() to only return DELETE action results

    // TODO : .ToObservable() Rx tests
}
