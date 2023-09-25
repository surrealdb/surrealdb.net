using System.Text.Json.Serialization;

namespace SurrealDb.Net.Tests;

public class SessionInfo
{
	[JsonPropertyName("ns")]
	public string Namespace { get; set; } = string.Empty;

	[JsonPropertyName("db")]
	public string Database { get; set; } = string.Empty;
}

public class ConnectTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldConnect(string url)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            using var client = surrealDbClientGenerator.Create(url);

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

			using var client = SurrealDbWsClient.New("localhost:8000", dbInfo.Namespace, dbInfo.Database);

			await client.Connect();

			var response = await client.Query("SELECT * FROM $session;");

			var list = response.GetValue<List<SessionInfo>>(0)!;
			result = list[0];
		};

		await func.Should().NotThrowAsync();

		result.Should().NotBeNull();

		result!.Namespace.Should().Be(dbInfo!.Namespace);
		result!.Database.Should().Be(dbInfo!.Database);
	}
}
