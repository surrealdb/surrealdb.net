using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

public class CreateBench : BaseBenchmark
{
	const string httpUrl = "http://localhost:8000";

	private SurrealDbClientGenerator? _surrealDbClientGenerator;
	private PostFaker? _postFaker;

	private ISurrealDbClient? _surrealdbHttpClient;
	private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;

	[GlobalSetup]
	public async Task GlobalSetup()
	{
		_surrealDbClientGenerator = new SurrealDbClientGenerator();
		_postFaker = new PostFaker();

		Use(_surrealDbClientGenerator.GenerateDatabaseInfo());

		await CreatePostTable(httpUrl);

		_surrealdbHttpClient = new SurrealDbClient(httpUrl);
		await InitializeSurrealDbClient(_surrealdbHttpClient);

		_surrealdbHttpClientWithHttpClientFactory = _surrealDbClientGenerator.Create(httpUrl);
		await InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory);
	}

	[GlobalCleanup]
	public async Task GlobalCleanup()
	{
		if (_surrealDbClientGenerator is not null)
			await _surrealDbClientGenerator.DisposeAsync();
	}

	[Benchmark]
	public Task<Post> HttpClient()
	{
		var generatedPost = _postFaker!.Generate();
		var post = new Post { Title = generatedPost.Title, Content = generatedPost.Content };

		return _surrealdbHttpClient!.Create("post", post);
	}

    [Benchmark]
    public Task<Post> HttpClientWithFactory()
	{
		var generatedPost = _postFaker!.Generate();
		var post = new Post { Title = generatedPost.Title, Content = generatedPost.Content };

		return _surrealdbHttpClientWithHttpClientFactory!.Create("post", post);
	}
}
