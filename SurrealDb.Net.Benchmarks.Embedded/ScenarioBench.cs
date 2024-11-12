using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Embedded.SurrealKv;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class ScenarioBench : BaseEmbeddedBenchmark
{
    private SurrealDbMemoryClient? _memoryClient;
    private SurrealDbRocksDbClient? _rocksDbClient;
    private SurrealDbKvClient? _surrealKvClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new(NamingPolicy);
        _rocksDbClient = new("rocks/scenario.db", NamingPolicy);
        _surrealKvClient = new("surrealkv/scenario.db", NamingPolicy);

        ISurrealDbClient[] clients = [_memoryClient, _rocksDbClient, _surrealKvClient];

        foreach (var client in clients)
        {
            client
                .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
                .GetAwaiter()
                .GetResult();
            CreateEcommerceTables(client, DefaultDatabaseInfo).GetAwaiter().GetResult();
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        ISurrealDbClient[] clients = [_memoryClient!, _rocksDbClient!, _surrealKvClient!];

        foreach (var client in clients)
        {
            DropEcommerceTables(client, DefaultDatabaseInfo).GetAwaiter().GetResult();
            client.Dispose();
        }
    }

    [Benchmark]
    public async Task<List<ProductAlsoPurchased>> Memory()
    {
        return await BenchmarkRuns.Scenario(_memoryClient!);
    }

    [Benchmark]
    public async Task<List<ProductAlsoPurchased>> RocksDb()
    {
        return await BenchmarkRuns.Scenario(_rocksDbClient!);
    }

    [Benchmark]
    public async Task<List<ProductAlsoPurchased>> SurrealKv()
    {
        return await BenchmarkRuns.Scenario(_surrealKvClient!);
    }
}
