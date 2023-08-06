using BenchmarkDotNet.Attributes;

namespace SurrealDb.Benchmarks;

public class QueryBench : BaseBenchmark
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
					InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
					await _surrealdbHttpClient.Connect();
					break;
				case 1:
					_surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(HttpUrl);
					InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
					await _surrealdbHttpClientWithHttpClientFactory.Connect();
					break;
				case 2:
					_surrealdbWsTextClient = new SurrealDbClient(WsUrl);
					InitializeSurrealDbClient(_surrealdbWsTextClient, dbInfo);
					await _surrealdbWsTextClient.Connect();
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

	const string Query = @"
SELECT * FROM post;
SELECT * FROM $auth;

BEGIN TRANSACTION;

CREATE post;

CANCEL TRANSACTION;";

	[Benchmark]
	public Task<List<Post>> Http()
	{
		return Run(_surrealdbHttpClient!);
	}

	[Benchmark]
    public Task<List<Post>> HttpWithClientFactory()
	{
		return Run(_surrealdbHttpClientWithHttpClientFactory!);
	}

	[Benchmark]
	public Task<List<Post>> WsText()
	{
		return Run(_surrealdbWsTextClient!);
	}

	[Benchmark]
	public Task<List<Post>> WsBinary()
	{
		throw new NotImplementedException();
	}

	private static async Task<List<Post>> Run(ISurrealDbClient surrealDbClient)
	{
		var response = await surrealDbClient.Query(Query);
		return response.GetValue<List<Post>>(0)!;
	}
}
