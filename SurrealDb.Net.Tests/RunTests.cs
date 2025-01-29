namespace SurrealDb.Net.Tests;

public class RunTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldRunFunctionWithoutArgs(string connectionString)
    {
        DateTime? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.Run<DateTime>("time::now");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldRunFunctionWithArgs(string connectionString)
    {
        string? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.Run<string>("string::repeat", ["test", 3]);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result.Should().Be("testtesttest");
    }
}
