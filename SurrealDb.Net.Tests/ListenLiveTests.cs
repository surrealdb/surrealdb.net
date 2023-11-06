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
}
