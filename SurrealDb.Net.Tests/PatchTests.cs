using System.Text;
using System.Text.Json;
using SurrealDb.Net.Internals.Json;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Tests;

public class PatchTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldPatchExistingPost(string connectionString)
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
            await client.RawQuery(query);

            var jsonPatchDocument = new JsonPatchDocument<Post>
            {
                Options = SurrealDbSerializerOptions.GetDefaultSerializerFromPolicy(
                    JsonNamingPolicy.SnakeCaseLower
                )
            };
            jsonPatchDocument.Replace(x => x.Content, "[Edit] This is my first article");

            result = await client.Patch(("post", "first"), jsonPatchDocument);

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

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData(
        "Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON",
        Skip = "To be removed"
    )]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData(
        "Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON",
        Skip = "To be removed"
    )]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldPatchExistingPostUsingStringRecordId(string connectionString)
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
            await client.RawQuery(query);

            var jsonPatchDocument = new JsonPatchDocument<Post>
            {
                Options = SurrealDbSerializerOptions.GetDefaultSerializerFromPolicy(
                    JsonNamingPolicy.SnakeCaseLower
                )
            };
            jsonPatchDocument.Replace(x => x.Content, "[Edit] This is my first article");

            result = await client.Patch(new StringRecordId("post:first"), jsonPatchDocument);

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
