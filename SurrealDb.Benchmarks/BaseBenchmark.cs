using System.Text;

namespace SurrealDb.Benchmarks;

public class BaseBenchmark
{
	protected string Namespace { get; set; } = "test";
	protected string Database { get; set; } = "test";

	protected void Use(DatabaseInfo databaseInfo)
	{
		Namespace = databaseInfo.Namespace;
		Database = databaseInfo.Database;
	}

	protected async Task InitializeSurrealDbClient(ISurrealDbClient client)
	{
		await client.Signin(new RootAuth { Username = "root", Password = "root" });
		await client.Use(Namespace, Database);
	}

	protected async Task CreatePostTable(string url)
	{
		var client = new SurrealDbClient(url);
		await InitializeSurrealDbClient(client);

		string query = GetPostQueryContent();
		await client.Query(query);
	}

	protected async Task<List<GeneratedPost>> SeedData(string url, int count = 1000)
	{
		var client = new SurrealDbClient(url);
		await InitializeSurrealDbClient(client);

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

	protected async Task<Post> GetFirstPost(string httpUrl)
	{
		var client = new SurrealDbClient(httpUrl);
		await InitializeSurrealDbClient(client);

		var posts = await client.Select<Post>("post");
		return posts.First();
	}

	private static string GetPostQueryContent()
	{
		string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
		return File.ReadAllText(filePath, Encoding.UTF8);
	}
}
