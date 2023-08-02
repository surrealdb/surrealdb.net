using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

public class DeleteBench : BaseBenchmark
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
	public Task HttpClient()
	{
		return _surrealdbHttpClient!.Delete("post");
	}

    [Benchmark]
    public Task HttpClientWithFactory()
	{
		return _surrealdbHttpClientWithHttpClientFactory!.Delete("post");
	}
}
