﻿using BenchmarkDotNet.Attributes;
using SurrealDb.Embedded.InMemory;
using SurrealDb.Embedded.RocksDb;
using SurrealDb.Net.Benchmarks.Models;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class QueryBench : BaseEmbeddedBenchmark
{
    private readonly IEnumerable<GeneratedPost> _generatedPosts = new PostFaker().Generate(1000);

    private SurrealDbMemoryClient? _memoryClient;
    private SurrealDbRocksDbClient? _rocksDbClient;

    [IterationSetup]
    public void Setup()
    {
        _memoryClient = new(NamingPolicy);
        _rocksDbClient = new("rocks/query.db", NamingPolicy);

        ISurrealDbClient[] clients = [_memoryClient, _rocksDbClient];

        foreach (var client in clients)
        {
            client
                .Use(DefaultDatabaseInfo.Namespace, DefaultDatabaseInfo.Database)
                .GetAwaiter()
                .GetResult();
            SeedData(client, DefaultDatabaseInfo, _generatedPosts).GetAwaiter().GetResult();
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryClient?.Dispose();
        _rocksDbClient?.Dispose();
    }

    [Benchmark]
    public async Task<List<Post>> Memory()
    {
        return await BenchmarkRuns.Query(_memoryClient!);
    }

    [Benchmark]
    public async Task<List<Post>> RocksDb()
    {
        return await BenchmarkRuns.Query(_rocksDbClient!);
    }
}
