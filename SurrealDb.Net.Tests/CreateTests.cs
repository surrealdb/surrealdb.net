﻿using System.Text;

namespace SurrealDb.Net.Tests;

public class CreateTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreateEmptyWithoutData(string connectionString)
    {
        IEnumerable<Empty>? list = null;
        Empty? result = null;

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

            result = await client.Create<Empty>("empty");

            list = await client.Select<Empty>("empty");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreatePostWithAutogeneratedId(string connectionString)
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

            result = await client.Create("post", post);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(3);

        result.Should().NotBeNull();
        result!.Title.Should().Be("A new article");
        result!.Content.Should().Be("This is a new article created using the .NET SDK");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreatePostWithPredefinedId(string connectionString)
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
                Id = new RecordIdOfString("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK"
            };

            result = await client.Create(post);

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
    public async Task ShouldCreateMultiplePosts(string connectionString)
    {
        IEnumerable<Post>? list = null;

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

            var posts = new List<Post>
            {
                new Post
                {
                    Id = new RecordIdOfString("post", "A"),
                    Title = "An article",
                    Content = "This is a new article"
                },
                new Post
                {
                    Id = new RecordIdOfString("post", "B"),
                    Title = "An article",
                    Content = "This is a new article"
                },
                new Post
                {
                    Id = new RecordIdOfString("post", "C"),
                    Title = "An article",
                    Content = "This is a new article"
                }
            };

            var tasks = posts.Select(p => client.Create(p));
            await Task.WhenAll(tasks.ToArray());

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(5);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreatePostUsingStringRecordId(string connectionString)
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

            result = await client.Create<Post, Post>(new StringRecordId("post:new"), post, default);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(3);

        result.Should().NotBeNull();
        result!.Title.Should().Be("A new article");
        result!.Content.Should().Be("This is a new article created using the .NET SDK");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }
}
