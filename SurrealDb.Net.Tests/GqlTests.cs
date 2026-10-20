namespace SurrealDb.Net.Tests;

public class GqlTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.3")]
    public async Task ShouldRunGqlQuery(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Post);

        var response = await client.Gql("MATCH (p:post) RETURN p.title AS title");

        response.EnsureAllOks();
        response
            .GetValue<List<Dictionary<string, string>>>(0)!
            .Select(row => row["title"])
            .Should()
            .BeEquivalentTo("First article", "Second article");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.3")]
    public async Task ShouldRunGqlQueryWithParameters(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Post);

        var response = await client.Gql(
            "MATCH (p:post) WHERE p.title = $title RETURN p.title AS title",
            new Dictionary<string, object?> { { "title", "First article" } }
        );

        response
            .GetValue<List<Dictionary<string, string>>>(0)!
            .Select(row => row["title"])
            .Should()
            .Equal("First article");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.3")]
    public async Task ShouldReportGqlErrors(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        Func<Task> func = () => client.Gql("MATCH (p:post RETURN p");

        await func.Should().ThrowAsync<Exception>();
    }
}
