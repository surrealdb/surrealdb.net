using System.Runtime.CompilerServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Benchmarks;

public class UpsertBench : BaseBenchmark
{
    private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];
    private readonly PostFaker _postFaker = new();
    private readonly Post[] _posts = new Post[4];

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

            switch (index)
            {
                case 0:
                    _surrealdbHttpClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(HttpUrl)
                            .WithNamingPolicy(NamingPolicy)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        $"Endpoint={HttpUrl}",
                        funcJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    _surrealdbWsTextClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(WsUrl)
                            .WithNamingPolicy(NamingPolicy)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbWsTextClient, dbInfo);
                    await _surrealdbWsTextClient.Connect();
                    break;
                case 3:
                    if (JsonSerializer.IsReflectionEnabledByDefault)
                    {
                        _surrealdbWsBinaryClient = new SurrealDbClient(
                            SurrealDbOptions
                                .Create()
                                .WithEndpoint(WsUrl)
                                .WithNamingPolicy(NamingPolicy)
                                .WithSerialization(SerializationConstants.CBOR)
                                .Build()
                        );
                        InitializeSurrealDbClient(_surrealdbWsBinaryClient, dbInfo);
                        await _surrealdbWsBinaryClient.Connect();
                    }
                    break;
            }
            await SeedData(WsUrl, dbInfo);

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
        _surrealdbWsTextClient?.Dispose();
        _surrealdbWsBinaryClient?.Dispose();
    }

    [Benchmark]
    public Task<Post> Http()
    {
        return Run(_surrealdbHttpClient!, _postFaker, _posts[0]);
    }

    [Benchmark]
    public Task<Post> HttpWithClientFactory()
    {
        return Run(_surrealdbHttpClientWithHttpClientFactory!, _postFaker, _posts[1]);
    }

    [Benchmark]
    public Task<Post> WsText()
    {
        return Run(_surrealdbWsTextClient!, _postFaker, _posts[2]);
    }

    [Benchmark]
    public Task<Post> WsBinary()
    {
        return Run(_surrealdbWsBinaryClient!, _postFaker, _posts[2]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<Post> Run(ISurrealDbClient surrealDbClient, PostFaker postFaker, Post post)
    {
        var generatedPost = postFaker.Generate();

        post.Title = generatedPost.Title;
        post.Content = generatedPost.Content;

        return surrealDbClient.Upsert(post);
    }
}
