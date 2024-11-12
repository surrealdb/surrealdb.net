using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Embedded.SurrealKv;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class UpsertBench : BaseEmbeddedBenchmark
{
    private readonly PostFaker _postFaker = new();
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);

    private SurrealDbMemoryClient? _memoryClient;
    private SurrealDbRocksDbClient? _rocksDbClient;
    private SurrealDbKvClient? _surrealKvClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new(NamingPolicy);
        _rocksDbClient = new("rocks/upsert.db", NamingPolicy);
        _surrealKvClient = new("surrealkv/upsert.db", NamingPolicy);

        ISurrealDbClient[] clients = [_memoryClient, _rocksDbClient, _surrealKvClient];

        foreach (var client in clients)
        {
            client
                .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
                .GetAwaiter()
                .GetResult();
            SeedData(client, DefaultDatabaseInfo, _generatedPosts).GetAwaiter().GetResult();
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        ISurrealDbClient[] clients = [_memoryClient!, _rocksDbClient!, _surrealKvClient!];

        foreach (var client in clients)
        {
            ClearData(client, DefaultDatabaseInfo).GetAwaiter().GetResult();
            client.Dispose();
        }
    }

    [Benchmark]
    public async Task<Post> Memory()
    {
        var post = await GetFirstPost(_memoryClient!, DefaultDatabaseInfo);
        return await BenchmarkRuns.Upsert(_memoryClient!, _postFaker, post);
    }

    [Benchmark]
    public async Task<Post> RocksDb()
    {
        var post = await GetFirstPost(_rocksDbClient!, DefaultDatabaseInfo);
        return await BenchmarkRuns.Upsert(_rocksDbClient!, _postFaker, post);
    }

    [Benchmark]
    public async Task<Post> SurrealKv()
    {
        var post = await GetFirstPost(_surrealKvClient!, DefaultDatabaseInfo);
        return await BenchmarkRuns.Upsert(_surrealKvClient!, _postFaker, post);
    }
}
