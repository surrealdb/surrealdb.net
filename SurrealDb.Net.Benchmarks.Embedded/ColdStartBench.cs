using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class ColdStartBench : BaseEmbeddedBenchmark
{
    [Benchmark]
    public async Task MemoryConstructor()
    {
        using var client = new SurrealDbMemoryClient(NamingPolicy);
        await client.Connect();
    }
}
