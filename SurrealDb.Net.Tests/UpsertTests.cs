using System.Text;

namespace SurrealDb.Net.Tests;

public class UpsertTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldCreateNewPost(string url)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/post.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var post = new Post
            {
                Id = new Thing("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK"
            };

            result = await client.Upsert(post);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(3);

        result.Should().NotBeNull();
        result!.Title.Should().Be("A new article");
        result!.Content.Should().Be("This is a new article created using the .NET SDK");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");

        var anotherPost = list!.First(r => r.Id!.Id == "another");

        anotherPost.Should().NotBeNull();
        anotherPost!.Title.Should().Be("A new article");
        anotherPost!.Content.Should().Be("This is a new article created using the .NET SDK");
        anotherPost!.CreatedAt.Should().NotBeNull();
        anotherPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUpdateExistingPost(string url)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/post.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Id = new Thing("post", "first"),
                Title = "[Updated] First article",
                Content = "[Edit] This is my first article",
                CreatedAt = existingCreatedAt,
                Status = existingStatus
            };

            result = await client.Upsert(post);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("[Updated] First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }
}
