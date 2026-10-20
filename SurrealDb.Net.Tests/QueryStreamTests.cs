using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public class QueryStreamTests
{
    private sealed class Item
    {
        [Column("n")]
        public int N { get; set; }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldStreamRowsAndValues(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.ApplySchemaAsync(SurrealSchemaFile.Post);

        var frames = new List<SurrealDbQueryStreamFrame>();
        await foreach (
            var frame in client.RawQueryStream(
                "SELECT * FROM post WHERE status == $status; RETURN 1 + 2;",
                new Dictionary<string, object?> { { "status", "DRAFT" } }
            )
        )
        {
            frames.Add(frame);
        }

        frames[0].Should().BeOfType<SurrealDbQueryStreamBeginFrame>();
        frames[^1].Should().BeOfType<SurrealDbQueryStreamEndFrame>();

        var end = (SurrealDbQueryStreamEndFrame)frames[^1];
        end.IsOk.Should().BeTrue();
        end.Results.Should().Be(2);

        var posts = frames
            .OfType<SurrealDbQueryStreamRowsFrame>()
            .Where(f => f.Index == 0)
            .SelectMany(f => f.GetValues<Post>())
            .ToList();
        posts.Should().HaveCount(2);

        var value = frames.OfType<SurrealDbQueryStreamValueFrame>().Single(f => f.Index == 1);
        value.GetValue<int>().Should().Be(3);

        var finished = frames.OfType<SurrealDbQueryStreamFinishedFrame>().ToList();
        finished.Select(f => f.Index).Should().BeEquivalentTo([0, 1]);
        finished.Should().OnlyContain(f => f.IsOk);
        finished.Single(f => f.Index == 0).Single.Should().BeFalse();
        finished.Single(f => f.Index == 1).Single.Should().BeTrue();

        // 💡 The frames of a statement come before its finished frame
        int valueIndex = frames.IndexOf(value);
        int finishedIndex = frames.IndexOf(finished.Single(f => f.Index == 1));
        valueIndex.Should().BeLessThan(finishedIndex);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldStreamRowsInOrder(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        const int count = 2000;
        var items = Enumerable.Range(1, count).Select(n => new Item { N = n }).ToList();
        await client.RawQuery(
            "INSERT INTO item $items RETURN NONE;",
            new Dictionary<string, object?> { { "items", items } }
        );

        var values = new List<int>();
        await foreach (var frame in client.RawQueryStream("SELECT n FROM item ORDER BY n;"))
        {
            if (frame is SurrealDbQueryStreamRowsFrame rows)
            {
                values.AddRange(rows.GetValues<Item>().Select(item => item.N));
            }
        }

        values.Should().Equal(Enumerable.Range(1, count));
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldReportFailedStatement(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        var frames = new List<SurrealDbQueryStreamFrame>();
        await foreach (var frame in client.RawQueryStream("RETURN 1; THROW 'boom'; RETURN 2;"))
        {
            frames.Add(frame);
        }

        var finished = frames
            .OfType<SurrealDbQueryStreamFinishedFrame>()
            .OrderBy(f => f.Index)
            .ToList();
        finished.Should().HaveCount(3);

        finished[0].IsOk.Should().BeTrue();
        finished[1].IsOk.Should().BeFalse();
        finished[1].Error!.Message.Should().Contain("boom");
        finished[2].IsOk.Should().BeTrue();

        frames
            .OfType<SurrealDbQueryStreamValueFrame>()
            .Single(f => f.Index == 2)
            .GetValue<int>()
            .Should()
            .Be(2);
        frames.OfType<SurrealDbQueryStreamEndFrame>().Single().IsOk.Should().BeTrue();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldThrowWhenQueryIsInvalid(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        Func<Task> func = async () =>
        {
            await foreach (var _ in client.RawQueryStream("SELEC * FROM item;")) { }
        };

        await func.Should().ThrowAsync<SurrealDbException>();

        // 💡 The connection is still usable
        var response = await client.RawQuery("RETURN 1;");
        response.GetValue<int>(0).Should().Be(1);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.3")]
    public async Task ShouldStreamInManyFrames(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        const int count = 5000;
        var items = Enumerable.Range(1, count).Select(n => new Item { N = n }).ToList();
        await client.RawQuery(
            "INSERT INTO item $items RETURN NONE;",
            new Dictionary<string, object?> { { "items", items } }
        );

        int rowsFrames = 0;
        await foreach (var frame in client.RawQueryStream("SELECT n FROM item;"))
        {
            if (frame is SurrealDbQueryStreamRowsFrame)
            {
                rowsFrames++;
            }
        }

        rowsFrames.Should().BeGreaterThan(1);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldStopWhenLeftEarly(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        const int count = 5000;
        var items = Enumerable.Range(1, count).Select(n => new Item { N = n }).ToList();
        await client.RawQuery(
            "INSERT INTO item $items RETURN NONE;",
            new Dictionary<string, object?> { { "items", items } }
        );

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int rows = 0;
            await foreach (
                var frame in client.RawQueryStream("SELECT n FROM item; SELECT n FROM item;")
            )
            {
                if (frame is SurrealDbQueryStreamRowsFrame rowsFrame)
                {
                    rows += rowsFrame.GetValues<Item>().Count;
                    break;
                }
            }

            rows.Should().BePositive();
        }

        // 💡 Neither the streams left early nor their cancellation disturb the next requests
        var response = await client.RawQuery("RETURN 1;");
        response.GetValue<int>(0).Should().Be(1);

        int total = 0;
        await foreach (var frame in client.RawQueryStream("SELECT n FROM item;"))
        {
            if (frame is SurrealDbQueryStreamRowsFrame rowsFrame)
            {
                total += rowsFrame.GetValues<Item>().Count;
            }
        }

        total.Should().Be(count);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.3")]
    public async Task ShouldCommitAfterLeavingTransactionalStreamEarly(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        const int count = 5000;
        var items = Enumerable.Range(1, count).Select(n => new Item { N = n }).ToList();
        await client.RawQuery(
            "INSERT INTO item $items RETURN NONE;",
            new Dictionary<string, object?> { { "items", items } }
        );

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);
        await session.SignIn(
            new RootAuth { Username = options.Username!, Password = options.Password! }
        );

        await using var transaction = await session.BeginTransaction();
        await transaction.RawQuery("CREATE marker:1;");

        await foreach (var frame in transaction.RawQueryStream("SELECT n FROM item;"))
        {
            if (frame is SurrealDbQueryStreamRowsFrame)
            {
                break;
            }
        }

        // 💡 The commit waits for the stream to end on the server
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await transaction.Commit(cts.Token);

        var markers = await client.RawQuery("SELECT * FROM marker;");
        markers.GetValue<List<object>>(0).Should().HaveCount(1);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.3")]
    public async Task ShouldWaitForTransactionalStreamBeingReadBeforeCommitting(
        string connectionString
    )
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        const int count = 5000;
        var items = Enumerable.Range(1, count).Select(n => new Item { N = n }).ToList();
        await client.RawQuery(
            "INSERT INTO item $items RETURN NONE;",
            new Dictionary<string, object?> { { "items", items } }
        );

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);
        await session.SignIn(
            new RootAuth { Username = options.Username!, Password = options.Password! }
        );

        await using var transaction = await session.BeginTransaction();
        await transaction.RawQuery("CREATE marker:1;");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Task? commitTask = null;
        bool isCommittedWhileReading = false;
        int total = 0;
        SurrealDbQueryStreamEndFrame? endFrame = null;

        await foreach (var frame in transaction.RawQueryStream("SELECT n FROM item;"))
        {
            if (commitTask is null)
            {
                // 💡 Commits from elsewhere while the stream is still being read
                commitTask = Task.Run(() => transaction.Commit(cts.Token));
                await Task.Delay(500);
                isCommittedWhileReading = commitTask.IsCompleted;
            }

            switch (frame)
            {
                case SurrealDbQueryStreamRowsFrame rows:
                    total += rows.GetValues<Item>().Count;
                    break;
                case SurrealDbQueryStreamEndFrame end:
                    endFrame = end;
                    break;
            }
        }

        await commitTask!;

        isCommittedWhileReading.Should().BeFalse();
        endFrame.Should().NotBeNull();
        endFrame!.IsOk.Should().BeTrue();
        total.Should().Be(count);

        var markers = await client.RawQuery("SELECT * FROM marker;");
        markers.GetValue<List<object>>(0).Should().HaveCount(1);
    }
}
