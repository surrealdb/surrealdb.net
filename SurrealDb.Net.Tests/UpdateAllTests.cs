using System.Text;

namespace SurrealDb.Net.Tests;

public class UpdateAllTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldUpdateAllRecords(string url)
    {
        List<Post>? list = null;
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

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var postUpdate = new Post
            {
                Title = "# Title",
                Content = "# Content",
                CreatedAt = now,
                Status = "PUBLISHED"
            };

            list = await client.Select<Post>("post");

            results = await client.UpdateAll("post", postUpdate);
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        results.Should().NotBeNull();

        var expected = list!.Select(
            item =>
                new Post
                {
                    Id = item.Id,
                    Title = "# Title",
                    Content = "# Content",
                    CreatedAt = now,
                    Status = "PUBLISHED",
                }
        );

        results.Should().BeEquivalentTo(expected);
    }
}
