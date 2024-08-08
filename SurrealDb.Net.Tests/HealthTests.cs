namespace SurrealDb.Net.Tests;

public class HealthTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldBeTrueOnAValidServer(string connectionString)
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
    [InlineData("Endpoint=ws://localhost:1234/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://localhost:1234/rpc;Serialization=CBOR")]
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
