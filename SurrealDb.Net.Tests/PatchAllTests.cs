#if NET8_0_OR_GREATER
using System.Text;
using System.Text.Json;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Tests;

public class PatchAllTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldPatchAllRecords(string connectionString)
    {
        IEnumerable<Post>? list = null;
        IEnumerable<Post>? results = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var jsonPatchDocument = new JsonPatchDocument<Post>
            {
                Options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                },
            };
            jsonPatchDocument.Replace(x => x.Content, "[Edit] Oops");

            list = await client.Select<Post>("post");

            results = await client.Patch("post", jsonPatchDocument);
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        var expected = list!.Select(item => new Post
        {
            Id = item.Id,
            Title = item.Title,
            Content = "[Edit] Oops",
            CreatedAt = item.CreatedAt,
            Status = item.Status,
        });

        results.Should().BeEquivalentTo(expected);
    }
}
#endif
