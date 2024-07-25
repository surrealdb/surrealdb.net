using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Benchmarks.Models;
using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Remote;

public class QueryBench : BaseRemoteBenchmark
{
    private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);

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
                            .WithNamingPolicy(NamingPolicy)
                            .Build()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        $"Endpoint={HttpUrl}"
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    if (JsonSerializer.IsReflectionEnabledByDefault)
                    {
                        _surrealdbWsBinaryClient = new SurrealDbClient(
                            SurrealDbOptions
                                .Create()
                                .WithEndpoint(WsUrl)
                                .WithNamingPolicy(NamingPolicy)
                                .Build()
                        );
                        InitializeSurrealDbClient(_surrealdbWsBinaryClient, dbInfo);
                        await _surrealdbWsBinaryClient.Connect();
                    }
                    break;
            }
            await SeedData(WsUrl, dbInfo, _generatedPosts);
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
    public Task<List<Post>> Http()
    {
        return BenchmarkRuns.Query(_surrealdbHttpClient!);
    }

    [Benchmark]
    public Task<List<Post>> HttpWithClientFactory()
    {
        return BenchmarkRuns.Query(_surrealdbHttpClientWithHttpClientFactory!);
    }

    [Benchmark]
    public Task<List<Post>> Ws()
    {
        return BenchmarkRuns.Query(_surrealdbWsBinaryClient!);
    }
}
