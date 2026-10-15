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

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
        };

        await func.Should().NotThrowAsync();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldClearNamespaceAndDatabase(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.Use(null, null);

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string?[]>(0).Should().Equal(null, null);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldOnlyChangeNamespace(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.UseNs("other");

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string?[]>(0).Should().Equal("other", dbInfo.Database);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldOnlyChangeDatabase(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.UseDb("other");

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string?[]>(0).Should().Equal(dbInfo.Namespace, "other");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldClearDatabaseOnly(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.UseDb(null);

        var response = await client.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string?[]>(0).Should().Equal(dbInfo.Namespace, null);
    }
}
