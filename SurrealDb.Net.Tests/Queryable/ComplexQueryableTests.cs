namespace SurrealDb.Net.Tests.Queryable;

public class ComplexQueryableTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSelectWithComplexQuery(string connectionString)
    {
        IEnumerable<string>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client
                .Select<Post>("post")
                .Where(p => p.Status == "DRAFT")
                .OrderBy(p => p.Id)
                .ThenBy(p => p.Title)
                .Skip(1)
                .Take(5)
                .Select(p => p.Content)
                .ToListAsync();
        };

        await func.Should().NotThrowAsync();

        // 2 (DRAFT) - (1 skipped) = 1
        result.Should().NotBeNull().And.HaveCount(1);
    }
}
