using System.Text;
using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net.Tests;

public class DeleteTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldDeletePostTable(string connectionString)
    {
        IEnumerable<Post>? list = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldDeletePostRecord(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Delete(("post", "first"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.FirstOrDefault(p => p.Id! == ("post", "first"));

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldDeletePostRecordUsingRecordId(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Delete(("post", "first"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.FirstOrDefault(p => p.Id! == ("post", "first"));

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldTryToDeleteInexistentRecord(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Delete(("post", "inexistent"));

            list = await client.Select<Post>("post");
        };

        if (version.Major >= 3)
        {
            await func.Should()
                .ThrowAsync<SurrealDbException>()
                .WithMessage("Expected a single result output when using the ONLY keyword");
            return;
        }

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);
        result.Should().BeFalse();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldDeletePostRecordUsingStringRecordId(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Delete(new StringRecordId("post:first"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.FirstOrDefault(p => p.Id!.DeserializeId<string>() == "first");

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }
}
