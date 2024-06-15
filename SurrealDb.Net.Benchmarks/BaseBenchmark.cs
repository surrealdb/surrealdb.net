using System.Text;
using SurrealDb.Net.Benchmarks.Constants;

namespace SurrealDb.Net.Benchmarks;

public class BaseBenchmark
{
    protected string NamingPolicy { get; } = "SnakeCase";

    protected BaseBenchmark()
    {
        bool isNativeAotRuntime = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(EnvVariablesConstants.NativeAotRuntime)
        );
    }

    protected async Task CreatePostTable(ISurrealDbClient client, DatabaseInfo databaseInfo)
    {
        string query = GetPostQueryContent();
        await client.RawQuery(query);
    }

    protected async Task CreateEcommerceTables(ISurrealDbClient client, DatabaseInfo databaseInfo)
    {
        string query = GetEcommerceQueryContent();
        await client.RawQuery(query);
    }

    protected async Task<IEnumerable<GeneratedPost>> SeedData(
        ISurrealDbClient client,
        DatabaseInfo databaseInfo,
        IEnumerable<GeneratedPost> posts
    )
    {
        var tasks = new List<Task>();

        foreach (var post in posts)
        {
            string statement =
                $"CREATE post SET title = \"{post.Title}\", content = \"{post.Content}\";";
            tasks.Add(client.RawQuery(statement));
        }

        await Task.WhenAll(tasks);

        return posts;
    }

    protected async Task<Post> GetFirstPost(ISurrealDbClient client, DatabaseInfo databaseInfo)
    {
        var posts = await client.Select<Post>("post");
        return posts.First();
    }

    private static string GetPostQueryContent()
    {
        return GetFileContent("Schemas/post.surql");
    }

    private static string GetEcommerceQueryContent()
    {
        return GetFileContent("Schemas/ecommerce.surql");
    }

    private static string GetFileContent(string path)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        return File.ReadAllText(filePath, Encoding.UTF8);
    }
}
