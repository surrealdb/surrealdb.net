﻿using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Helpers;

namespace SurrealDb.Net.Benchmarks;

public class ColdStartBenchmark : BaseBenchmark
{
    private ServiceProvider? _serviceProvider;
    private DatabaseInfo? _databaseInfo;

    private List<ISurrealDbClient> _clients = new();

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
            _serviceProvider!.GetRequiredService<IHttpClientFactory>(),
            appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
        );
        _clients.Add(client);

        InitializeSurrealDbClient(client, _databaseInfo!);
        await client.Connect();
    }

    [Benchmark]
    public async Task WsConstructor()
    {
        var client = new SurrealDbClient(
            WsUrl,
            NamingPolicy,
            appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
        );
        _clients.Add(client);

        InitializeSurrealDbClient(client, _databaseInfo!);
        await client.Connect();
    }
}
