using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class ScenarioBench : BaseEmbeddedBenchmark
{
    private SurrealDbMemoryClient? _memoryClient;
    private SurrealDbRocksDbClient? _rocksDbClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new(NamingPolicy);
        _rocksDbClient = new("rocks/scenario.db", NamingPolicy);

        ISurrealDbClient[] clients = [_memoryClient, _rocksDbClient];

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
        _memoryClient?.Dispose();
        _rocksDbClient?.Dispose();
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
}
