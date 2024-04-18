using System.Text;
using System.Text.Json;
using SurrealDb.Net.Internals.Json;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Tests;

public class PatchAllTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldPatchAllRecords(string connectionString)
    {
        IEnumerable<Post>? list = null;
        IEnumerable<Post>? results = null;

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

            var jsonPatchDocument = new JsonPatchDocument<Post>
            {
                Options = SurrealDbSerializerOptions.GetDefaultSerializerFromPolicy(
                    JsonNamingPolicy.SnakeCaseLower
                )
            };
            jsonPatchDocument.Replace(x => x.Content, "[Edit] Oops");

            list = await client.Select<Post>("post");

            results = await client.PatchAll("post", jsonPatchDocument);
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
