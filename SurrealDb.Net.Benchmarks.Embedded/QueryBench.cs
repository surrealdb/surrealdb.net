using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class QueryBench : BaseEmbeddedBenchmark
{
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);

    private SurrealDbMemoryClient? _memoryClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new SurrealDbMemoryClient(NamingPolicy);
        _memoryClient
            .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
            .GetAwaiter()
            .GetResult();
        SeedData(_memoryClient, DefaultDatabaseInfo, _generatedPosts).GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryClient?.Dispose();
    }

    [Benchmark]
    public async Task<List<Post>> Memory()
    {
        return await BenchmarkRuns.Query(_memoryClient!);
    }
}
