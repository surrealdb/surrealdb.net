using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public partial class LiveQueryTests
{
    [Fact]
    public async Task ShouldExcludeCloseResultWithGetResults()
    {
        const string url = "ws://localhost:8000/rpc";

        var allResults = new List<SurrealDbLiveQueryResponse>();
        var filteredResults = new List<SurrealDbLiveQueryResponse>();

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

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.GetResults(cts.Token))
                {
                    filteredResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);

                var record = await client.Create("test", new TestRecord { Value = 1 });
                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
                await client.Delete(record.Id!);

                while (allResults.Count < 3)
                {
                    await Task.Delay(20);
                }

                await liveQuery.KillAsync();

                while (allResults.Count < 4)
                {
                    await Task.Delay(20);
                }

                cts.Cancel();
            });

            await Task.Delay(_timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(4);
        filteredResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task ShouldGetCreatedRecords()
    {
        const string url = "ws://localhost:8000/rpc";

        var allResults = new List<SurrealDbLiveQueryResponse>();
        var filteredResults = new List<TestRecord>();

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

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.GetCreatedRecords(cts.Token))
                {
                    filteredResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);

                var record = await client.Create("test", new TestRecord { Value = 1 });
                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
                await client.Delete(record.Id!);

                while (allResults.Count < 3)
                {
                    await Task.Delay(20);
                }

                await liveQuery.KillAsync();

                while (allResults.Count < 4)
                {
                    await Task.Delay(20);
                }

                cts.Cancel();
            });

            await Task.Delay(_timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(4);
        filteredResults.Should().HaveCount(1);

        var result = filteredResults.First();

        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task ShouldGetUpdatedRecords()
    {
        const string url = "ws://localhost:8000/rpc";

        var allResults = new List<SurrealDbLiveQueryResponse>();
        var filteredResults = new List<TestRecord>();

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

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.GetUpdatedRecords(cts.Token))
                {
                    filteredResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);

                var record = await client.Create("test", new TestRecord { Value = 1 });
                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
                await client.Delete(record.Id!);

                while (allResults.Count < 3)
                {
                    await Task.Delay(20);
                }

                await liveQuery.KillAsync();

                while (allResults.Count < 4)
                {
                    await Task.Delay(20);
                }

                cts.Cancel();
            });

            await Task.Delay(_timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(4);
        filteredResults.Should().HaveCount(1);

        var result = filteredResults.First();

        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task ShouldGetDeletedIds()
    {
        const string url = "ws://localhost:8000/rpc";

        TestRecord? record = null;

        var allResults = new List<SurrealDbLiveQueryResponse>();
        var filteredResults = new List<Thing>();

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

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.WithCancellation(cts.Token))
                {
                    allResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var result in liveQuery.GetDeletedIds(cts.Token))
                {
                    filteredResults.Add(result);
                }
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);

                record = await client.Create("test", new TestRecord { Value = 1 });
                await client.Upsert(new TestRecord { Id = record.Id, Value = 2 });
                await client.Delete(record.Id!);

                while (allResults.Count < 3)
                {
                    await Task.Delay(20);
                }

                await liveQuery.KillAsync();

                while (allResults.Count < 4)
                {
                    await Task.Delay(20);
                }

                cts.Cancel();
            });

            await Task.Delay(_timeout);

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                throw new Exception("Timeout");
            }
        };

        await func.Should().NotThrowAsync();

        allResults.Should().HaveCount(4);
        filteredResults.Should().HaveCount(1);

        var result = filteredResults.First();

        result.Should().Be(record!.Id);
    }
}
