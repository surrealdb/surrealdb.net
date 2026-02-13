using System.Text;
using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net.Tests;

public class UpdateTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldNotCreateNewPost(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var post = new Post
            {
                Id = ("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            result = await client.Update(post);

            list = await client.Select<Post>("post");
        };

        if (version.Major >= 3)
        {
            await func.Should()
                .ThrowAsync<SurrealDbException>()
                .WithMessage("Expected a single result output when using the ONLY keyword");
            return;
        }

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);
        result.Should().BeNull();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUpdateExistingPost(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Id = ("post", "first"),
                Title = "[Updated] First article",
                Content = "[Edit] This is my first article",
                CreatedAt = existingCreatedAt,
                Status = existingStatus,
            };

            result = await client.Update(post);

            list = await client.Select<Post>("post");
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("[Updated] First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldNotCreateNewPostUsingStringRecordId(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var post = new Post
            {
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            result = await client.Update<Post, Post>(new StringRecordId("post:another"), post);

            list = await client.Select<Post>("post");
        };

        if (version.Major >= 3)
        {
            await func.Should()
                .ThrowAsync<SurrealDbException>()
                .WithMessage("Expected a single result output when using the ONLY keyword");
            return;
        }

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);
        result.Should().BeNull();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUpdateExistingPostUsingStringRecordId(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Title = "[Updated] First article",
                Content = "[Edit] This is my first article",
                CreatedAt = existingCreatedAt,
                Status = existingStatus,
            };

            result = await client.Update<Post, Post>(new StringRecordId("post:first"), post);

            list = await client.Select<Post>("post");
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("[Updated] First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }
}
