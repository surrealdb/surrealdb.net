namespace SurrealDb.Net.Tests;

public class UseTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUseTestDatabase(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
        };

        await func.Should().NotThrowAsync();
    }
}
