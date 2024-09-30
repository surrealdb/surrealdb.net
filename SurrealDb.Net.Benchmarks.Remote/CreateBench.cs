using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Benchmarks.Models;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Remote;

// TODO : Create a SurrealDbBenchmarkContext class

public class CreateBench : BaseRemoteBenchmark
{
    private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];
    private readonly PostFaker _postFaker = new();

    private ISurrealDbClient? _surrealdbHttpClient;
    private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;
    private ISurrealDbClient? _surrealdbWsTextClient;
    private ISurrealDbClient? _surrealdbWsBinaryClient;

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
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(HttpUrl)
                            .WithNamespace(dbInfo.Namespace)
                            .WithDatabase(dbInfo.Database)
                            .WithUsername("root")
                            .WithPassword("root")
                            .WithNamingPolicy(NamingPolicy)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        $"Endpoint={HttpUrl};NS={dbInfo.Namespace};DB={dbInfo.Database};user=root;pass=root",
                        funcJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    _surrealdbWsTextClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(WsUrl)
                            .WithNamespace(dbInfo.Namespace)
                            .WithDatabase(dbInfo.Database)
                            .WithUsername("root")
                            .WithPassword("root")
                            .WithNamingPolicy(NamingPolicy)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    await _surrealdbWsTextClient.Connect();
                    break;
                case 3:
                    if (JsonSerializer.IsReflectionEnabledByDefault)
                    {
                        _surrealdbWsBinaryClient = new SurrealDbClient(
                            SurrealDbOptions
                                .Create()
                                .WithEndpoint(WsUrl)
                                .WithNamespace(dbInfo.Namespace)
                                .WithDatabase(dbInfo.Database)
                                .WithUsername("root")
                                .WithPassword("root")
                                .WithNamingPolicy(NamingPolicy)
                                .WithSerialization(SerializationConstants.CBOR)
                                .Build()
                        );
                        await _surrealdbWsBinaryClient.Connect();
                    }
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
        _surrealdbWsBinaryClient?.Dispose();
    }

    [Benchmark]
    public Task<Post> Http()
    {
        return BenchmarkRuns.Create(_surrealdbHttpClient!, _postFaker);
    }

    [Benchmark]
    public Task<Post> HttpWithClientFactory()
    {
        return BenchmarkRuns.Create(_surrealdbHttpClientWithHttpClientFactory!, _postFaker);
    }

    [Benchmark]
    public Task<Post> WsText()
    {
        return BenchmarkRuns.Create(_surrealdbWsTextClient!, _postFaker);
    }

    [Benchmark]
    public Task<Post> WsBinary()
    {
        return BenchmarkRuns.Create(_surrealdbWsBinaryClient!, _postFaker);
    }
}
