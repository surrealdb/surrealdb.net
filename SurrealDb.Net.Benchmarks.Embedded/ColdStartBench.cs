﻿using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class ColdStartBench : BaseEmbeddedBenchmark
{
    [Benchmark]
    public async Task MemoryConstructor()
    {
        using var client = new SurrealDbMemoryClient(NamingPolicy);
        await client.Connect();
    }

    [Benchmark]
    public async Task RocksDbConstructor()
    {
        using var client = new SurrealDbRocksDbClient("rocks/constructor.db", NamingPolicy);
        await client.Connect();
    }
}
