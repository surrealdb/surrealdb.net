using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Benchmarks.Models;
using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Remote;

public class UpsertBench : BaseRemoteBenchmark
{
    private readonly SurrealDbClientGenerator?[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];
    private readonly PostFaker _postFaker = new();
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);
    private readonly Post[] _posts = new Post[4];

    private ISurrealDbClient? _surrealdbHttpClient;
    private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;
    private ISurrealDbClient? _surrealdbWsBinaryClient;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        for (int index = 0; index < 3; index++)
        {
            var clientGenerator = new SurrealDbClientGenerator();
            var dbInfo = clientGenerator.GenerateDatabaseInfo();

            switch (index)
            {
                case 0:
                    _surrealdbHttpClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(HttpUrl)
                            .WithNamespace(dbInfo.Namespace)
                            .WithDatabase(dbInfo.Database)
                            .WithUsername("root")
                            .WithPassword("root")
                            .WithNamingPolicy(NamingPolicy)
                            .Build()
                    );
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        $"Endpoint={HttpUrl};NS={dbInfo.Namespace};DB={dbInfo.Database};user=root;pass=root"
                    );
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    _surrealdbWsBinaryClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(WsUrl)
                            .WithNamespace(dbInfo.Namespace)
                            .WithDatabase(dbInfo.Database)
                            .WithUsername("root")
                            .WithPassword("root")
                            .WithNamingPolicy(NamingPolicy)
                            .Build()
                    );
                    await _surrealdbWsBinaryClient.Connect();
                    break;
            }
            await SeedData(WsUrl, dbInfo, _generatedPosts);

            _posts[index] = await GetFirstPost(WsUrl, dbInfo);
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
        _surrealdbWsBinaryClient?.Dispose();
    }

    [Benchmark]
    public Task<Post> Http()
    {
        return BenchmarkRuns.Upsert(_surrealdbHttpClient!, _postFaker, _posts[0]);
    }

    [Benchmark]
    public Task<Post> HttpWithClientFactory()
    {
        return BenchmarkRuns.Upsert(
            _surrealdbHttpClientWithHttpClientFactory!,
            _postFaker,
            _posts[1]
        );
    }

    [Benchmark]
    public Task<Post> Ws()
    {
        return BenchmarkRuns.Upsert(_surrealdbWsBinaryClient!, _postFaker, _posts[2]);
    }
}
