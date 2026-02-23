using System.Linq.Expressions;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.LiveQuery.Tests;

public class KillLiveQueryTests
{
    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldAutomaticallyKillLiveQueryWhenDisposed(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

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

        Expression<Func<SurrealDbException, bool>> validErrorMessage = ex =>
            ex.Message.Contains(
                "There was a problem with the database: Can not execute KILL statement using id"
            )
            || ex.Message.Contains("KILL statement uuid did not exist") // 1.x
            || ex.Message.Contains("Can not execute KILL statement using id '$id'") // 2.0.x
            || ex.Message.Contains(
                $"Cannot execute KILL statement using id: u'{liveQueryUuid}'"
            ) // 3.0.x
        ;

        await liveQueryAlreadyKilledFunc
            .Should()
            .ThrowAsync<SurrealDbException>()
            .Where(validErrorMessage);
    }

    [Test]
    [Arguments("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldManuallyKillLiveQuery(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.RawQuery("DEFINE TABLE test SCHEMALESS;");

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

        Expression<Func<SurrealDbException, bool>> validErrorMessage = ex =>
            ex.Message.Contains(
                "There was a problem with the database: Can not execute KILL statement using id"
            )
            || ex.Message.Contains("KILL statement uuid did not exist") // 1.x
            || ex.Message.Contains("Can not execute KILL statement using id '$id'") // 2.0.x
            || ex.Message.Contains(
                $"Cannot execute KILL statement using id: u'{liveQueryUuid}'"
            ) // 3.0.x
        ;

        await liveQueryAlreadyKilledFunc
            .Should()
            .ThrowAsync<SurrealDbException>()
            .Where(validErrorMessage);
    }
}
