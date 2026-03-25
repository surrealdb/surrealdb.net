namespace SurrealDb.Net.Tests.Queryable;

/// <summary>
/// Sandbox mode used to test any query
/// </summary>
public class DebugQueryableTests : BaseQueryableTests
{
    /// <summary>
    /// A sample test to try the query generation.
    /// </summary>
    [Test]
    [Skip("Sandbox")]
    public void QueryGenerator()
    {
        string query = ToSurql(Posts.Where(p => p.CreatedAt > DateTime.Now));

        query
            .Should()
            .Be(
                """
                SELECT content, created_at, id, status, title FROM post WHERE created_at > time::now()
                """
            );
    }

    /// <summary>
    /// A sample test to try a query against a memory instance.
    /// </summary>
    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [Skip("Sandbox")]
    public async Task Sandbox(string connectionString)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client
                .Select<Post>("post")
                .Where(p => p.CreatedAt > DateTime.Now)
                .ToListAsync();
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(0);
    }
}
