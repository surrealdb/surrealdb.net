using Dahomey.Cbor.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Net.Tests;

public class SessionInfo
{
    [CborProperty("ns")]
    public string Namespace { get; set; } = string.Empty;

    [CborProperty("db")]
    public string Database { get; set; } = string.Empty;
}

public class ConnectTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldConnect(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            await using var client = surrealDbClientGenerator.Create(connectionString);

            await client.Connect();
        };

        await func.Should().NotThrowAsync();
    }

    [Test]
    public async Task ShouldConnectAndApplyConfiguration()
    {
        DatabaseInfo? dbInfo = null;
        SessionInfo? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = new SurrealDbClient(
                new SurrealDbOptions
                {
                    Endpoint = "ws://127.0.0.1:8000/rpc",
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                }
            );

            await client.Connect();

            var response = await client.Query($"SELECT * FROM $session;");

            var list = response.GetValue<List<SessionInfo>>(0)!;
            result = list[0];
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();

        result!.Namespace.Should().Be(dbInfo!.Namespace);
        result!.Database.Should().Be(dbInfo!.Database);
    }
}
