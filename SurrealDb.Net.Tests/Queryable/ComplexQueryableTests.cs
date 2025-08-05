using System.Text;

namespace SurrealDb.Net.Tests.Queryable;

public class ComplexQueryableTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [Skip("TODO")]
    public async Task ShouldSelectWithComplexQuery(string connectionString)
    {
        IEnumerable<string>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/post.surql"
            );
            string fileContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

            string query = fileContent;

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.RawQuery(query);

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

        result.Should().NotBeNull().And.HaveCount(2);
    }
}
