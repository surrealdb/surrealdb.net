namespace SurrealDb.Tests;

public class VersionTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc")]
	public async Task ShouldGetVersion(string url)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

		using var client = surrealDbClientGenerator.Create(url);
        string result = await client.Version();

        result.Should().Be("surrealdb-1.0.0+20230913.54aedcd");
    }
}
