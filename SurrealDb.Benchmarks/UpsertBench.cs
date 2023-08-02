using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

public class UpsertBench : BaseBenchmark
{
	const string httpUrl = "http://localhost:8000";

	private SurrealDbClientGenerator? _surrealDbClientGenerator;
	private PostFaker? _postFaker;

	private ISurrealDbClient? _surrealdbHttpClient;
	private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;

	private Post? _post;

	[GlobalSetup]
	public async Task GlobalSetup()
	{
		_surrealDbClientGenerator = new SurrealDbClientGenerator();
		_postFaker = new PostFaker();

		Use(_surrealDbClientGenerator.GenerateDatabaseInfo());

		_surrealdbHttpClient = new SurrealDbClient(httpUrl);
		await InitializeSurrealDbClient(_surrealdbHttpClient);

		_surrealdbHttpClientWithHttpClientFactory = _surrealDbClientGenerator.Create(httpUrl);
		await InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory);

		await SeedData(httpUrl);

		_post = await GetFirstPost(httpUrl);
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

		_post!.Title = generatedPost.Title;
		_post!.Content = generatedPost.Content;

		return _surrealdbHttpClient!.Upsert(_post);
	}

    [Benchmark]
    public Task<Post> HttpClientWithFactory()
	{
		var generatedPost = _postFaker!.Generate();

		_post!.Title = generatedPost.Title;
		_post!.Content = generatedPost.Content;

		return _surrealdbHttpClientWithHttpClientFactory!.Upsert(_post);
	}
}
