﻿using System.Text;

namespace SurrealDb.Net.Tests;

public class DeleteTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldDeletePostTable(string connectionString)
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
            await client.RawQuery(query);

            await client.Delete("post");

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldDeletePostRecord(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

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
            await client.RawQuery(query);

            result = await client.Delete(("post", "first"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.FirstOrDefault(p => p.Id!.Id == "first");

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldDeletePostRecordUsingThing(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

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
            await client.RawQuery(query);

            result = await client.Delete(("post", "first"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.FirstOrDefault(p => p.Id!.Id == "first");

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldTryToDeleteInexistentRecord(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

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
            await client.RawQuery(query);

            result = await client.Delete(("post", "inexistent"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON", Skip = "To be removed")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON", Skip = "To be removed")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldDeletePostRecordUsingStringRecordId(string connectionString)
    {
        IEnumerable<Post>? list = null;
        bool? result = null;

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
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.RawQuery(query);

            result = await client.Delete(new StringRecordId("post:first"));

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        var firstPost = list!.FirstOrDefault(p => p.Id!.Id == "first");

        firstPost.Should().BeNull();

        result.Should().BeTrue();
    }
}
