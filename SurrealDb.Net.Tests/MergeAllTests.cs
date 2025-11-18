using System.Text;

namespace SurrealDb.Net.Tests;

public class MergeAllTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldMergeAllRecords(string connectionString)
    {
        IEnumerable<Post>? list = null;
        IEnumerable<Post>? results = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var merge = new PostMergeData { Content = "[Edit] Oops" };

            list = await client.Select<Post>("post").ToListAsync();

            results = await client.Merge<PostMergeData, Post>("post", merge);
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        var expected = list!.Select(item => new Post
        {
            Id = item.Id,
            Title = item.Title,
            Content = "[Edit] Oops",
            CreatedAt = item.CreatedAt,
            Status = item.Status,
        });

        results.Should().BeEquivalentTo(expected);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldMergeAllRecordsUsingDictionary(string connectionString)
    {
        IEnumerable<Post>? list = null;
        IEnumerable<Post>? results = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var recordId = new RecordIdOfString("post", "first");
            var data = new Dictionary<string, object> { { "content", "[Edit] Oops" } };

            list = await client.Select<Post>("post").ToListAsync();

            results = await client.Merge<Post>("post", data);
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        var expected = list!.Select(item => new Post
        {
            Id = item.Id,
            Title = item.Title,
            Content = "[Edit] Oops",
            CreatedAt = item.CreatedAt,
            Status = item.Status,
        });

        results.Should().BeEquivalentTo(expected);
    }
}
