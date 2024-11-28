using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.LiveQuery.Tests;

public class KillTests
{
    [Fact]
    public async Task ShouldNotBeSupportedOnHttpProtocol()
    {
        const string connectionString = "Endpoint=http://127.0.0.1:8000";

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var liveQueryUuid = Guid.NewGuid();

            await client.Kill(liveQueryUuid);
        };

        await func.Should().ThrowAsync<NotSupportedException>();
    }

    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldKillActiveLiveQueryOnWsProtocol(string connectionString)
    {
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

            await client.Kill(liveQueryUuid);
        };

        await func.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldFailToKillInexistantLiveQueryOnWsProtocol(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);
        var liveQueryUuid = Guid.NewGuid();

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Kill(liveQueryUuid);
        };

        string errorMessage = version switch
        {
            { Major: 1 }
                => "There was a problem with the database: Can not execute KILL statement using id 'KILL statement uuid did not exist'",
            { Major: 2, Minor: 0 }
                => "There was a problem with the database: Can not execute KILL statement using id '$id'",
            _
                => $"There was a problem with the database: Can not execute KILL statement using id 'u'{liveQueryUuid}''"
        };

        await func.Should().ThrowAsync<SurrealDbException>().WithMessage(errorMessage);
    }
}
