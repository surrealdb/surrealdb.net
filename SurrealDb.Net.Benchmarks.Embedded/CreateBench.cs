using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Embedded.SurrealKv;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class CreateBench : BaseEmbeddedBenchmark
{
    private readonly PostFaker _postFaker = new();

    private SurrealDbMemoryClient? _memoryClient;
    private SurrealDbRocksDbClient? _rocksDbClient;
    private SurrealDbKvClient? _surrealKvClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new(NamingPolicy);
        _rocksDbClient = new("rocks/create.db", NamingPolicy);
        _surrealKvClient = new("surrealkv/create.db", NamingPolicy);

        ISurrealDbClient[] clients = [_memoryClient, _rocksDbClient, _surrealKvClient];

        foreach (var client in clients)
        {
            client
                .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
                .GetAwaiter()
                .GetResult();
            CreatePostTable(client, DefaultDatabaseInfo).GetAwaiter().GetResult();
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        ISurrealDbClient[] clients = [_memoryClient!, _rocksDbClient!, _surrealKvClient!];

        foreach (var client in clients)
        {
            DropPostTable(client, DefaultDatabaseInfo).GetAwaiter().GetResult();
            client.Dispose();
        }
    }

    [Benchmark]
    public async Task<Post> Memory()
    {
        return await BenchmarkRuns.Create(_memoryClient!, _postFaker);
    }

    [Benchmark]
    public async Task<Post> RocksDb()
    {
        return await BenchmarkRuns.Create(_rocksDbClient!, _postFaker);
    }

    [Benchmark]
    public async Task<Post> SurrealKv()
    {
        return await BenchmarkRuns.Create(_surrealKvClient!, _postFaker);
    }
}
