namespace SurrealDb.Net.Tests;

public class VersionTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldGetVersion(string url)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        using var client = surrealDbClientGenerator.Create(url);
        string result = await client.Version();

        result.Should().BeValidSemver("surrealdb-");
    }
}
