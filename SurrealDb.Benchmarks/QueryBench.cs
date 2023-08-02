using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

public class QueryBench : BaseBenchmark
{
	const string httpUrl = "http://localhost:8000";

	private SurrealDbClientGenerator? _surrealDbClientGenerator;

	private ISurrealDbClient? _surrealdbHttpClient;
	private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;

	[GlobalSetup]
	public async Task GlobalSetup()
	{
		_surrealDbClientGenerator = new SurrealDbClientGenerator();

		Use(_surrealDbClientGenerator.GenerateDatabaseInfo());

		_surrealdbHttpClient = new SurrealDbClient(httpUrl);
		await InitializeSurrealDbClient(_surrealdbHttpClient);

		_surrealdbHttpClientWithHttpClientFactory = _surrealDbClientGenerator.Create(httpUrl);
		await InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory);

		await SeedData(httpUrl);
	}

	[GlobalCleanup]
	public async Task GlobalCleanup()
	{
		if (_surrealDbClientGenerator is not null)
			await _surrealDbClientGenerator.DisposeAsync();
	}

	const string Query = @"
SELECT * FROM post;
SELECT * FROM $auth;

BEGIN TRANSACTION;

CREATE post;

CANCEL TRANSATION;";

	[Benchmark]
	public async Task<List<Post>> HttpClient()
	{
		var response = await _surrealdbHttpClient!.Query(Query);
		return response.GetValue<List<Post>>(0)!;
	}

    [Benchmark]
    public async Task<List<Post>> HttpClientWithFactory()
	{
		var response = await _surrealdbHttpClientWithHttpClientFactory!.Query(Query);
		return response.GetValue<List<Post>>(0)!;
	}
}
