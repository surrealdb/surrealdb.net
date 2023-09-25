using System.Text;

namespace SurrealDb.Net.Tests;

public class DeleteTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldDeletePostTable(string url)
    {
        List<Post>? list = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            await client.Delete("post");

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldDeletePostRecord(string url)
    {
        List<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            result = await client.Delete("post", "first");

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.Find(p => p.Id!.Id == "first");

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldDeletePostRecordUsingThing(string url)
    {
        List<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = new Thing("post", "first");

            result = await client.Delete(thing);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.Find(p => p.Id!.Id == "first");

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldTryToDeleteInexistentRecord(string url)
    {
        List<Post>? list = null;
        bool? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = new Thing("post", "inexistent");

            result = await client.Delete(thing);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);
        result.Should().BeFalse();
    }
}
