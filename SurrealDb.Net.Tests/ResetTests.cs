using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests;

public class ResetTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldResetNamespaceAndDatabase(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.Reset();

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string?[]>(0).Should().Equal(null, null);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldResetVariables(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.Set("answer", 42);

        await client.Reset();
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        var response = await client.RawQuery("RETURN $answer;");
        response.GetValue<int?>(0).Should().BeNull();
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldResetAuthentication(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        (await client.RawQuery("INFO FOR ROOT;")).EnsureAllOks();

        await client.Reset();

        Func<Task> infoForRoot = async () =>
            (await client.RawQuery("INFO FOR ROOT;")).EnsureAllOks();
        await infoForRoot.Should().ThrowAsync<SurrealDbException>();
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldNotRestoreStateAfterReconnection(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.Set("answer", 42);

        await client.Reset();
        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string?[]>(0).Should().Equal(null, null);

        await client.Use(dbInfo.Namespace, dbInfo.Database);

        response = await client.RawQuery("RETURN $answer;");
        response.GetValue<int?>(0).Should().BeNull();

        Func<Task> infoForRoot = async () =>
            (await client.RawQuery("INFO FOR ROOT;")).EnsureAllOks();
        await infoForRoot.Should().ThrowAsync<SurrealDbException>();
    }
}
