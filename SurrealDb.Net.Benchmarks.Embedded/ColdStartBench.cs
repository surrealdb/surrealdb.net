using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Embedded.SurrealKv;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class ColdStartBench : BaseEmbeddedBenchmark
{
    [Benchmark]
    public async Task MemoryConstructor()
    {
        await using var client = new SurrealDbMemoryClient(NamingPolicy);
        await client.Connect();
    }

    [Benchmark]
    public async Task RocksDbConstructor()
    {
        await using var client = new SurrealDbRocksDbClient("rocks/constructor.db", NamingPolicy);
        await client.Connect();
    }

    [Benchmark]
    public async Task SurrealKvConstructor()
    {
        await using var client = new SurrealDbKvClient("surrealkv/constructor.db", NamingPolicy);
        await client.Connect();
    }
}
