using BenchmarkDotNet.Attributes;

namespace SurrealDb.Net.Benchmarks;

public class SelectBench : BaseBenchmark
{
    private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];

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
                    _surrealdbHttpClient = new SurrealDbClient(
                        HttpUrl,
                        NamingPolicy,
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        HttpUrl,
                        funcJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    _surrealdbWsTextClient = new SurrealDbClient(
                        WsUrl,
                        NamingPolicy,
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
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

    // 💡 Currently ignored benchmark: GitHub workflow need values to store benchmark results
    public Task<List<Post>> WsBinary()
    {
        throw new NotImplementedException();
    }

    private static async Task<List<Post>> Run(ISurrealDbClient surrealDbClient)
    {
        var result = await surrealDbClient.Select<Post>("post");
        return result.ToList();
    }
}
