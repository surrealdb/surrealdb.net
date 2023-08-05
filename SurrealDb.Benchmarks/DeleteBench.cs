using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

public class DeleteBench : BaseBenchmark
{
	private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators = new SurrealDbClientGenerator[4];

	private ISurrealDbClient? _surrealdbHttpClient;
    private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;
	private ISurrealDbClient? _surrealdbWsTextClient;

	[GlobalSetup]
    public async Task GlobalSetup()
	{
		for (int index = 0; index < 4; index++)
		{
			var clientGenerator = new SurrealDbClientGenerator();
			var dbInfo = clientGenerator.GenerateDatabaseInfo();

			switch (index)
			{
				case 0:
					_surrealdbHttpClient = new SurrealDbClient(HttpUrl);
					await InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
					break;
				case 1:
					_surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(HttpUrl);
					await InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
					break;
				case 2:
					_surrealdbWsTextClient = new SurrealDbClient(WsUrl);
					await InitializeSurrealDbClient(_surrealdbWsTextClient, dbInfo, true);
					break;
			}

			await SeedData(WsUrl, dbInfo);
		}
	}

	[GlobalCleanup]
	public async Task GlobalCleanup()
	{
		foreach (var clientGenerator in _surrealDbClientGenerators!)
		{
			if (clientGenerator is not null)
				await clientGenerator.DisposeAsync();
		}

		_surrealdbHttpClient?.Dispose();
		_surrealdbHttpClientWithHttpClientFactory?.Dispose();
		_surrealdbWsTextClient?.Dispose();
	}

	[Benchmark]
	public Task Http()
	{
		return Run(_surrealdbHttpClient!);
	}

	[Benchmark]
    public Task HttpWithClientFactory()
	{
		return Run(_surrealdbHttpClientWithHttpClientFactory!);
	}

	[Benchmark]
	public Task WsText()
	{
		return Run(_surrealdbWsTextClient!);
	}

	[Benchmark]
	public Task WsBinary()
	{
		throw new NotImplementedException();
	}

	private static Task Run(ISurrealDbClient surrealDbClient)
	{
		return surrealDbClient.Delete("post");
	}
}
