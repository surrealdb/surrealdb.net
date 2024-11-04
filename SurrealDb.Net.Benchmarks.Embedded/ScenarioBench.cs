using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class ScenarioBench : BaseEmbeddedBenchmark
{
    private SurrealDbMemoryClient? _memoryClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new SurrealDbMemoryClient(NamingPolicy);
        _memoryClient
            .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
            .GetAwaiter()
            .GetResult();
        CreateEcommerceTables(_memoryClient, DefaultDatabaseInfo).GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryClient?.Dispose();
    }

    [Benchmark]
    public async Task<List<ProductAlsoPurchased>> Memory()
    {
        using var client = new SurrealDbMemoryClient(NamingPolicy);
        await client.Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database);

        return await BenchmarkRuns.Scenario(_memoryClient!);
    }
}
