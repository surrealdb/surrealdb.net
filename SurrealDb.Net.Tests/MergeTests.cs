using System.ComponentModel.DataAnnotations.Schema;

namespace SurrealDb.Net.Tests;

public class PostMergeData
{
    [Column("content")]
    public string Content { get; set; } = string.Empty;
}

public class PostMergeRecord : SurrealDbRecord
{
    [Column("content")]
    public string Content { get; set; } = string.Empty;
}

public class MergeTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldMergeExistingPost(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var merge = new PostMergeRecord
            {
                Id = ("post", "first"),
                Content = "[Edit] This is my first article",
            };

            result = await client.Merge<PostMergeRecord, Post>(merge);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldMergeFromDictionaryUsingThing(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var data = new Dictionary<string, object>
            {
                { "content", "[Edit] This is my first article" },
            };

            result = await client.Merge<Post>(("post", "first"), data);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldMergeFromDictionaryUsingStringRecordId(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var data = new Dictionary<string, object>
            {
                { "content", "[Edit] This is my first article" },
            };

            result = await client.Merge<Post>(new StringRecordId("post:first"), data);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }
}
