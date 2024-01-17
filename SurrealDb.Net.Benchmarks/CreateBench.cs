using BenchmarkDotNet.Attributes;

namespace SurrealDb.Net.Benchmarks;

// TODO : Create a SurrealDbBenchmarkContext class

public class CreateBench : BaseBenchmark
{
    private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];
    private readonly PostFaker _postFaker = new();

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

            await CreatePostTable(WsUrl, dbInfo);

            switch (index)
            {
                case 0:
                    _surrealdbHttpClient = new SurrealDbClient(
                        HttpUrl,
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        HttpUrl,
                        GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    _surrealdbWsTextClient = new SurrealDbClient(
                        WsUrl,
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbWsTextClient, dbInfo);
                    await _surrealdbWsTextClient.Connect();
                    break;
            }
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
    public Task<Post> Http()
    {
        return Run(_surrealdbHttpClient!);
    }

    [Benchmark]
    public Task<Post> HttpWithClientFactory()
    {
        return Run(_surrealdbHttpClientWithHttpClientFactory!);
    }

    [Benchmark]
    public Task<Post> WsText()
    {
        return Run(_surrealdbWsTextClient!);
    }

    // 💡 Currently ignored benchmark: GitHub workflow need values to store benchmark results
    public Task<Post> WsBinary()
    {
        throw new NotImplementedException();
    }

    private Task<Post> Run(ISurrealDbClient surrealDbClient)
    {
        var generatedPost = _postFaker!.Generate();
        var post = new Post { Title = generatedPost.Title, Content = generatedPost.Content };

        return surrealDbClient.Create("post", post);
    }
}
