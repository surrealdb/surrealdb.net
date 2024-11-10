using System.Text;

namespace SurrealDb.Net.Tests;

public class UpsertTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreateNewPost(string connectionString)
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

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var post = new Post
            {
                Id = ("post", "another"),
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

        var anotherPost = list!.First(p => p.Id! == ("post", "another"));

        anotherPost.Should().NotBeNull();
        anotherPost!.Title.Should().Be("A new article");
        anotherPost!.Content.Should().Be("This is a new article created using the .NET SDK");
        anotherPost!.CreatedAt.Should().NotBeNull();
        anotherPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldUpdateExistingPost(string connectionString)
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

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Id = ("post", "first"),
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

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreateNewPostUsingStringRecordId(string connectionString)
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

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var post = new Post
            {
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK"
            };

            result = await client.Upsert<Post, Post>(new StringRecordId("post:another"), post);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(3);

        result.Should().NotBeNull();
        result!.Title.Should().Be("A new article");
        result!.Content.Should().Be("This is a new article created using the .NET SDK");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");

        var anotherPost = list!.First(r => r.Id!.DeserializeId<string>() == "another");

        anotherPost.Should().NotBeNull();
        anotherPost!.Title.Should().Be("A new article");
        anotherPost!.Content.Should().Be("This is a new article created using the .NET SDK");
        anotherPost!.CreatedAt.Should().NotBeNull();
        anotherPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldUpdateExistingPostUsingStringRecordId(string connectionString)
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

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Title = "[Updated] First article",
                Content = "[Edit] This is my first article",
                CreatedAt = existingCreatedAt,
                Status = existingStatus
            };

            result = await client.Upsert<Post, Post>(new StringRecordId("post:first"), post);

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
