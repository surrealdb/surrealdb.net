namespace SurrealDb.Net.Tests;

public class HealthTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldBeTrueOnAValidServer(string url)
    {
        bool? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);

            response = await client.Health();
        };

        await func.Should().NotThrowAsync();

        response.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://localhost:1234")]
    [InlineData("ws://localhost:1234/rpc")]
    public async Task ShouldBeFalseOnAnInvalidServer(string url)
    {
        bool? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);

            response = await client.Health();
        };

        await func.Should().NotThrowAsync();

        response.Should().BeFalse();
    }
}
