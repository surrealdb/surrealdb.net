using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Tests.Fixtures;

public sealed class DatabaseInfo
{
    public string Namespace { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
}

internal sealed class DatabaseInfoFaker : Faker<DatabaseInfo>
{
    public DatabaseInfoFaker()
    {
        RuleFor(o => o.Namespace, f => f.Random.AlphaNumeric(40));
        RuleFor(o => o.Database, f => f.Random.AlphaNumeric(40));
    }
}

internal sealed class FilePathInfo
{
    public string Path { get; init; } = string.Empty;
}

internal sealed class FilePathFaker : Faker<FilePathInfo>
{
    public FilePathFaker()
    {
        RuleFor(o => o.Path, f => $"temp/{f.Random.AlphaNumeric(40)}");
    }
}

public sealed class SurrealDbClientGenerator : IDisposable, IAsyncDisposable
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

    private void GenerateRandomFilePath()
    {
        _folderPath = _filePathFaker.Generate().Path;
    }

    public SurrealDbClient Create(string connectionString)
    {
        Configure(connectionString, ServiceLifetime.Singleton);
        return _serviceProvider!.GetRequiredService<SurrealDbClient>();
    }

    public SurrealDbClientGenerator Configure(
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? funcJsonSerializerContexts = null
    )
    {
        _options = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .WithNamingPolicy("SnakeCase")
            .Build();

        if (_options.Endpoint is "rocksdb://" or "surrealkv://")
        {
            GenerateRandomFilePath();

            _options = SurrealDbOptions
                .Create()
                .FromConnectionString(connectionString)
                .WithNamingPolicy("SnakeCase")
                .WithEndpoint($"{_options.Endpoint}{_folderPath}")
                .Build();
        }

        _serviceProvider = new ServiceCollection()
            .AddSurreal(_options, lifetime)
            .AddInMemoryProvider()
            .AddRocksDbProvider()
            .AddSurrealKvProvider()
            .And.BuildServiceProvider(validateScopes: true);

        return this;
    }

    public IServiceScope? CreateScope()
    {
        return _serviceProvider?.CreateScope();
    }

    public DatabaseInfo GenerateDatabaseInfo()
    {
        _databaseInfo = _databaseInfoFaker.Generate();
        return _databaseInfo;
    }

    public static async Task<SemVersion> GetSurrealTestVersion(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        return (await client.Version()).ToSemver();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_options is not null && !_options.IsEmbedded && _databaseInfo is not null)
        {
            await using var client = new SurrealDbClient("ws://127.0.0.1:8000/rpc", "SnakeCase");
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(_databaseInfo.Namespace, _databaseInfo.Database);

            await client.RawQuery($"REMOVE DATABASE `{_databaseInfo.Database}`;");
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
