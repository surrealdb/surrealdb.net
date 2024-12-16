using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.LiveQuery.Tests;

public class KillLiveQueryTests
{
    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldAutomaticallyKillLiveQueryWhenDisposed(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        Guid liveQueryUuid = Guid.Empty;

        Func<Task> createLiveQueryFunc = async () =>
        {
            var response = await client.RawQuery("LIVE SELECT * FROM test;");

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

        string errorMessage = version switch
        {
            { Major: 1 }
                => "There was a problem with the database: Can not execute KILL statement using id 'KILL statement uuid did not exist'",
            { Major: 2, Minor: 0 }
                => "There was a problem with the database: Can not execute KILL statement using id '$id'",
            _
                => $"There was a problem with the database: Can not execute KILL statement using id 'u'{liveQueryUuid}''"
        };

        await liveQueryAlreadyKilledFunc
            .Should()
            .ThrowAsync<SurrealDbException>()
            .WithMessage(errorMessage);
    }

    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldManuallyKillLiveQuery(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        SurrealDbLiveQuery<int>? liveQuery = null;
        Guid liveQueryUuid = Guid.Empty;

        Func<Task> createLiveQueryFunc = async () =>
        {
            var response = await client.RawQuery("LIVE SELECT * FROM test;");

            if (response.FirstResult is not SurrealDbOkResult okResult)
                throw new Exception("Expected a SurrealDbOkResult");

            liveQueryUuid = okResult.GetValue<Guid>();

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

        string errorMessage = version switch
        {
            { Major: 1 }
                => "There was a problem with the database: Can not execute KILL statement using id 'KILL statement uuid did not exist'",
            { Major: 2, Minor: 0 }
                => "There was a problem with the database: Can not execute KILL statement using id '$id'",
            _
                => $"There was a problem with the database: Can not execute KILL statement using id 'u'{liveQueryUuid}''"
        };

        await liveQueryAlreadyKilledFunc
            .Should()
            .ThrowAsync<SurrealDbException>()
            .WithMessage(errorMessage);
    }
}
