using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests;

public class SessionInfo
{
    [JsonPropertyName("ns")]
    [CborProperty("ns")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("db")]
    [CborProperty("db")]
    public string Database { get; set; } = string.Empty;
}

public class ConnectTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldConnect(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            using var client = surrealDbClientGenerator.Create(connectionString);

            await client.Connect();
        };

        await func.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ShouldConnectAndApplyConfiguration()
    {
        DatabaseInfo? dbInfo = null;
        SessionInfo? result = null;

        Func<Task> func = async () =>
        {
            using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = new SurrealDbClient("ws://127.0.0.1:8000/rpc");
            client.Configure(dbInfo.Namespace, dbInfo.Database);

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
