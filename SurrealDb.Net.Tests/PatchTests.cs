#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

namespace SurrealDb.Net.Tests;

[JsonSerializable(typeof(Post))]
public partial class PatchTestsJsonContext : JsonSerializerContext;

public class PatchTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldPatchExistingPost(string connectionString)
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

            var jsonPatchDocument = new JsonPatchDocument<Post>
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    TypeInfoResolver = PatchAllTestsJsonContext.Default,
                },
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

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldPatchExistingPostUsingStringRecordId(string connectionString)
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

            var jsonPatchDocument = new JsonPatchDocument<Post>
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    TypeInfoResolver = PatchAllTestsJsonContext.Default,
                },
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
#endif
