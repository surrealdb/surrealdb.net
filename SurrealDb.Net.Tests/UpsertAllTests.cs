using System.Text;
using Semver;

namespace SurrealDb.Net.Tests;

public class UpsertAllTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUpsertAllRecords(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Post>? list = null;
        IEnumerable<Post>? results = null;

        var now = DateTime.UtcNow;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var postUpdate = new Post
            {
                Title = "# Title",
                Content = "# Content",
                CreatedAt = now,
                Status = "PUBLISHED"
            };

            list = await client.Select<Post>("post");

            results = await client.Upsert("post", postUpdate);
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        results.Should().NotBeNull();

        if (SemVersion.CompareSortOrder(version, new SemVersion(2, 1)) >= 0)
        {
            // 💡 Since v2.1.0, creates a new random record instead of updating all records
            results.Should().HaveCount(1);

            var newRecord = results!.First();

            newRecord
                .Should()
                .BeEquivalentTo(
                    new Post
                    {
                        Id = newRecord.Id,
                        Title = "# Title",
                        Content = "# Content",
                        CreatedAt = now,
                        Status = "PUBLISHED",
                    }
                );
        }
        else
        {
            var expected = list!.Select(item => new Post
            {
                Id = item.Id,
                Title = "# Title",
                Content = "# Content",
                CreatedAt = now,
                Status = "PUBLISHED",
            });

            results.Should().BeEquivalentTo(expected);
        }
    }
}
