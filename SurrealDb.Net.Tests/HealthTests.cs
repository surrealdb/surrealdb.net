namespace SurrealDb.Net.Tests;

public class HealthTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldBeTrueOnAValidEndpoint(string connectionString)
    {
        bool? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            await using var client = surrealDbClientGenerator.Create(connectionString);

            response = await client.Health();
        };

        await func.Should().NotThrowAsync();

        response.Should().BeTrue();
    }

    [Test]
    [Arguments("Endpoint=http://localhost:1234")]
    [Arguments("Endpoint=ws://localhost:1234/rpc")]
    public async Task ShouldBeFalseOnAnInvalidServer(string connectionString)
    {
        bool? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            await using var client = surrealDbClientGenerator.Create(connectionString);

            response = await client.Health();
        };

        await func.Should().NotThrowAsync();

        response.Should().BeFalse();
    }
}
