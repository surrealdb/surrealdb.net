using System.Text;

namespace SurrealDb.Net.Tests;

public class UpdateAllTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldUpdateAllRecords(string connectionString)
    {
        IEnumerable<Post>? list = null;
        IEnumerable<Post>? results = null;

        var now = DateTime.UtcNow;

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

            var postUpdate = new Post
            {
                Title = "# Title",
                Content = "# Content",
                CreatedAt = now,
                Status = "PUBLISHED"
            };

            list = await client.Select<Post>("post");

            results = await client.Update("post", postUpdate);
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        results.Should().NotBeNull();

        var expected = list!.Select(item => new Post
        {
            Id = item.Id,
            Title = "# Title",
            Content = "# Content",
            CreatedAt = now,
            Status = "PUBLISHED",
        });

        results.Should().BeEquivalentTo(expected);
    }
}
