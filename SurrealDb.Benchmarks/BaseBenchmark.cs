using System.Text;

namespace SurrealDb.Benchmarks;

public class BaseBenchmark
{
	protected string HttpUrl { get; } = "http://localhost:8000";
	protected string WsUrl { get; } = "ws://localhost:8000/rpc";

	protected void InitializeSurrealDbClient(ISurrealDbClient client, DatabaseInfo databaseInfo)
	{
		client.Configure(databaseInfo.Namespace, databaseInfo.Database, "root", "root");
	}

	protected async Task CreatePostTable(string url, DatabaseInfo databaseInfo)
	{
		var client = new SurrealDbClient(url);
		InitializeSurrealDbClient(client, databaseInfo);

		string query = GetPostQueryContent();
		await client.Query(query);
	}

	protected async Task CreateEcommerceTables(string url, DatabaseInfo databaseInfo)
	{
		var client = new SurrealDbClient(url);
		InitializeSurrealDbClient(client, databaseInfo);

		string query = GetEcommerceQueryContent();
		await client.Query(query);
	}

	protected async Task<List<GeneratedPost>> SeedData(string url, DatabaseInfo databaseInfo, int count = 1000)
	{
		var client = new SurrealDbClient(url);
		InitializeSurrealDbClient(client, databaseInfo);

		var tasks = new List<Task>();

		var generatedPosts = new PostFaker().Generate(count);

		generatedPosts.ForEach((post) =>
		{
			string statement = $"CREATE post SET title = \"{post.Title}\", content = \"{post.Content}\";";
			tasks.Add(client.Query(statement));
		});

		await Task.WhenAll(tasks);

		return generatedPosts;
	}

	protected async Task<Post> GetFirstPost(string httpUrl, DatabaseInfo databaseInfo)
	{
		var client = new SurrealDbClient(httpUrl);
		InitializeSurrealDbClient(client, databaseInfo);

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
