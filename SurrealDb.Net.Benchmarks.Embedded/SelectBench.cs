using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Embedded.SurrealKv;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class SelectBench : BaseEmbeddedBenchmark
{
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);

    private SurrealDbMemoryClient? _memoryClient;
    private SurrealDbRocksDbClient? _rocksDbClient;
    private SurrealDbKvClient? _surrealKvClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new(NamingPolicy);
        _rocksDbClient = new("rocks/select.db", NamingPolicy);
        _surrealKvClient = new("surrealkv/select.db", NamingPolicy);

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
    public async Task<List<Post>> Memory()
    {
        return await BenchmarkRuns.Select(_memoryClient!);
    }

    [Benchmark]
    public async Task<List<Post>> RocksDb()
    {
        return await BenchmarkRuns.Select(_rocksDbClient!);
    }

    [Benchmark]
    public async Task<List<Post>> SurrealKv()
    {
        return await BenchmarkRuns.Select(_surrealKvClient!);
    }
}
