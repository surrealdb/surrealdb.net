namespace SurrealDb.Net.Tests;

public class UseTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseTestDatabase(string url)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
        };

        await func.Should().NotThrowAsync();
    }
}
