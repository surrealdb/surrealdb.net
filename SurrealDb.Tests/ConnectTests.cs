namespace SurrealDb.Tests;

public class ConnectTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
    public async Task ShouldConnect(string url)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            var client = surrealDbClientGenerator.Create(url);

            await client.Connect();
        };

        await func.Should().NotThrowAsync();
    }
}
