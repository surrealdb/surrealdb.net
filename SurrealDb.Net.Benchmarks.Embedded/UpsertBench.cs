using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class UpsertBench : BaseEmbeddedBenchmark
{
    private readonly PostFaker _postFaker = new();
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);

    private SurrealDbMemoryClient? _memoryClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new SurrealDbMemoryClient(NamingPolicy);
        InitializeSurrealDbClient(_memoryClient, DefaultDatabaseInfo);
        SeedData(_memoryClient, DefaultDatabaseInfo, _generatedPosts).GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryClient?.Dispose();
    }

    [Benchmark]
    public async Task<Post> Memory()
    {
        var post = await GetFirstPost(_memoryClient!, DefaultDatabaseInfo);
        return await BenchmarkRuns.Upsert(_memoryClient!, _postFaker, post);
    }
}
