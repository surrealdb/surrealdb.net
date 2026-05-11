using Microsoft.Extensions.DependencyInjection;
using Semver;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Tests.Fixtures;

public sealed class SurrealDbClientGenerator(ServiceLifetime lifetime = ServiceLifetime.Singleton)
    : IAsyncDisposable
{
    private static readonly DatabaseInfoFaker _databaseInfoFaker = new();
    private static readonly FilePathFaker _filePathFaker = new();

    private ServiceProvider? _serviceProvider;
    private DatabaseInfo? _databaseInfo;
    private SurrealDbOptions? _options;
    private string? _folderPath;

    static SurrealDbClientGenerator()
    {
        ClearTempFolder();
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        ClearTempFolder();
    }

    private static void ClearTempFolder()
    {
        if (Directory.Exists("temp"))
        {
            Directory.Delete("temp", true);
        }
    }

    public ISurrealDbClient Create(string connectionString, string? ns = null, string? db = null)
    {
        Configure(connectionString, ns, db);
        return GetClient();
    }

    public void Configure(string connectionString, string? ns = null, string? db = null)
    {
        var optionsBuilder = SurrealDbOptions.Create().FromConnectionString(connectionString);

        var temporaryOptions = optionsBuilder.Build();

        if (temporaryOptions.Endpoint is "rocksdb://" or "surrealkv://")
        {
            GenerateRandomFilePath();

            optionsBuilder = SurrealDbOptions
                .Create()
                .FromConnectionString(connectionString)
                .WithEndpoint($"{temporaryOptions.Endpoint}{_folderPath}");
        }

        if (ns is not null)
        {
            optionsBuilder = optionsBuilder.WithNamespace(ns);
        }
        if (db is not null)
        {
            optionsBuilder = optionsBuilder.WithDatabase(db);
        }

        _options = optionsBuilder.Build();

        _serviceProvider = new ServiceCollection()
            .AddSurreal(_options, lifetime: lifetime)
            .AddInMemoryProvider()
            .AddRocksDbProvider()
            .AddSurrealKvProvider()
            .And.BuildServiceProvider(validateScopes: true);
    }

    private void GenerateRandomFilePath()
    {
        _folderPath = _filePathFaker.Generate().Path;
    }

    private SurrealDbClient GetClient()
    {
        return _serviceProvider!.GetRequiredService<SurrealDbClient>();
    }

    public AsyncServiceScope CreateAsyncScope()
    {
        return _serviceProvider!.CreateAsyncScope();
    }

    public DatabaseInfo GenerateDatabaseInfo()
    {
        _databaseInfo = _databaseInfoFaker.Generate();
        return _databaseInfo;
    }

    public static async Task<SemVersion> GetSurrealTestVersion(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        surrealDbClientGenerator.Create(connectionString);
        await using var client = surrealDbClientGenerator.GetClient();

        return (await client.Version()).ToSemver();
    }

    public async ValueTask DisposeAsync()
    {
        if (_options is not null && !_options.IsEmbedded && _databaseInfo is not null)
        {
            await CleanDatabaseAsync(_databaseInfo);
        }
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    private static async ValueTask CleanDatabaseAsync(DatabaseInfo databaseInfo)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await using var client = new SurrealDbClient("ws://127.0.0.1:8000/rpc");
        await client.SignIn(new RootAuth { Username = "root", Password = "root" }, cts.Token);
        await client.Use(databaseInfo.Namespace, databaseInfo.Database, cts.Token);

        await client.RawQuery(
            $"REMOVE DATABASE `{databaseInfo.Database}`;",
            cancellationToken: cts.Token
        );
    }
}
