using System.Text.Json;

namespace SurrealDb.Net.Tests.Serializers.Json;

public class ConfigureJsonSerializerOptionsTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseCamelCasePolicyOnSelect(string url)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new Post
                {
                    Id = ("post", "first"),
                    Title = "First article",
                    Content = "This is my first article",
                    Status = "DRAFT",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            result = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();

        var firstPost = list.First(p => p.Id!.Id == "first");

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseCamelCasePolicyOnQuery(string url)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new Post
                {
                    Id = ("post", "first"),
                    Title = "First article",
                    Content = "This is my first article",
                    Status = "DRAFT",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var response = await client.Query($"SELECT * FROM post");

            result = response.GetValues<Post>(0);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();

        var firstPost = list.First(p => p.Id!.Id == "first");

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseKebabCasePolicyOnSelect(string url)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new Post
                {
                    Id = ("post", "first"),
                    Title = "First article",
                    Content = "This is my first article",
                    Status = "DRAFT",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            result = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();

        var firstPost = list.First(p => p.Id!.Id == "first");

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseKebabCasePolicyOnQuery(string url)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new Post
                {
                    Id = ("post", "first"),
                    Title = "First article",
                    Content = "This is my first article",
                    Status = "DRAFT",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var response = await client.Query($"SELECT * FROM post");

            result = response.GetValues<Post>(0);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();

        var firstPost = list.First(p => p.Id!.Id == "first");

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");
    }
}
