using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

[MemoryDiagnoser]
public class SelectBench : BaseBenchmark
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

	[Benchmark]
	public Task<List<Post>> HttpClient()
	{
		return _surrealdbHttpClient!.Select<Post>("post");
	}

    [Benchmark]
    public Task<List<Post>> HttpClientWithFactory()
	{
		return _surrealdbHttpClientWithHttpClientFactory!.Select<Post>("post");
	}
}
