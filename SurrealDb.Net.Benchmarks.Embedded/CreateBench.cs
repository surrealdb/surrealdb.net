using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class CreateBench : BaseEmbeddedBenchmark
{
    private readonly PostFaker _postFaker = new();

    private SurrealDbMemoryClient? _memoryClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new SurrealDbMemoryClient(NamingPolicy);
        _memoryClient
            .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
            .GetAwaiter()
            .GetResult();
        CreatePostTable(_memoryClient, DefaultDatabaseInfo).GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryClient?.Dispose();
    }

    [Benchmark]
    public async Task<Post> Memory()
    {
        return await BenchmarkRuns.Create(_memoryClient!, _postFaker);
    }
}
