using BenchmarkDotNet.Attributes;
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
    public async Task GlobalSetup()
    {
        var services = new ServiceCollection();

        string httpClientName = HttpClientHelper.GetHttpClientName(new Uri(HttpUrl));
        services.AddHttpClient(httpClientName);

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);

        await using var clientGenerator = new SurrealDbClientGenerator();
        _databaseInfo = clientGenerator.GenerateDatabaseInfo();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }

        await _serviceProvider!.DisposeAsync();
    }

    [Benchmark]
    public async Task HttpConstructor()
    {
        var options = new SurrealDbOptionsBuilder()
            .WithEndpoint(HttpUrl)
            .WithNamespace(_databaseInfo!.Namespace)
            .WithDatabase(_databaseInfo!.Database)
            .WithUsername("root")
            .WithPassword("root")
            .Build();

        var client = new SurrealDbClient(
            options,
            _serviceProvider!.GetService<IHttpClientFactory>()
        );
        _clients.Add(client);

        await client.Connect();
    }

    [Benchmark]
    public async Task WsConstructor()
    {
        var options = new SurrealDbOptionsBuilder()
            .WithEndpoint(HttpUrl)
            .WithNamespace(_databaseInfo!.Namespace)
            .WithDatabase(_databaseInfo!.Database)
            .WithUsername("root")
            .WithPassword("root")
            .Build();

        var client = new SurrealDbClient(options);
        _clients.Add(client);

        await client.Connect();
    }
}
