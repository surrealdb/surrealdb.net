namespace SurrealDb.Tests;

public class UseTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
    public async Task ShouldUseTestDatabase(string url)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            var client = surrealDbClientGenerator.Create(url);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
        };

        await func.Should().NotThrowAsync();
    }
}
