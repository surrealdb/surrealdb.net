namespace SurrealDb.Net.Tests;

public class HealthTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldBeTrueOnAValidEndpoint(string connectionString)
    {
        bool? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);

            response = await client.Health();
        };

        await func.Should().NotThrowAsync();

        response.Should().BeTrue();
    }

    [Theory]
    [InlineData("Endpoint=http://localhost:1234")]
    [InlineData("Endpoint=ws://localhost:1234/rpc")]
    public async Task ShouldBeFalseOnAnInvalidServer(string connectionString)
    {
        bool? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);

            response = await client.Health();
        };

        await func.Should().NotThrowAsync();

        response.Should().BeFalse();
    }
}
