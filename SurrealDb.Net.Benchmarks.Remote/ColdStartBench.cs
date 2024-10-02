﻿using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Remote;

public class ColdStartBench : BaseRemoteBenchmark
{
    private ServiceProvider? _serviceProvider;
    private DatabaseInfo? _databaseInfo;

    private readonly List<ISurrealDbClient> _clients = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();

        string httpClientName = HttpClientHelper.GetHttpClientName(new Uri(HttpUrl));
        services.AddHttpClient(httpClientName);

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);

        using var clientGenerator = new SurrealDbClientGenerator();
        _databaseInfo = clientGenerator.GenerateDatabaseInfo();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        _serviceProvider?.Dispose();
    }

    [Benchmark]
    public async Task HttpConstructor()
    {
        var client = new SurrealDbClient(
            HttpUrl,
            NamingPolicy,
            _serviceProvider!.GetRequiredService<IHttpClientFactory>()
        );
        _clients.Add(client);

        InitializeSurrealDbClient(client, _databaseInfo!);
        await client.Connect();
    }

    [Benchmark]
    public async Task WsConstructor()
    {
        var client = new SurrealDbClient(WsUrl, NamingPolicy);
        _clients.Add(client);

        InitializeSurrealDbClient(client, _databaseInfo!);
        await client.Connect();
    }
}
