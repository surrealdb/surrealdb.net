using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Tests.Fixtures;

public class DatabaseInfo
{
    public string Namespace { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}

public class DatabaseInfoFaker : Faker<DatabaseInfo>
{
    public DatabaseInfoFaker()
    {
        RuleFor(o => o.Namespace, f => f.Random.AlphaNumeric(40));
        RuleFor(o => o.Database, f => f.Random.AlphaNumeric(40));
    }
}

public sealed class SurrealDbClientGenerator : IDisposable, IAsyncDisposable
{
    private static readonly DatabaseInfoFaker _databaseInfoFaker = new();

    private ServiceProvider? _serviceProvider;
    private DatabaseInfo? _databaseInfo;
    private SurrealDbOptions? _options;

    public SurrealDbClient Create(string connectionString)
    {
        _options = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .WithNamingPolicy("SnakeCase")
            .Build();

        _serviceProvider = new ServiceCollection()
            .AddSurreal(_options)
            .AddInMemoryProvider()
            .And.BuildServiceProvider(validateScopes: true);

        return _serviceProvider.GetRequiredService<SurrealDbClient>();
    }

    public DatabaseInfo GenerateDatabaseInfo()
    {
        _databaseInfo = _databaseInfoFaker.Generate();
        return _databaseInfo;
    }

    public static async Task<SemVersion> GetSurrealTestVersion(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        using var client = surrealDbClientGenerator.Create(connectionString);

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
            using var client = new SurrealDbClient("ws://127.0.0.1:8000/rpc", "SnakeCase");
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(_databaseInfo.Namespace, _databaseInfo.Database);

            await client.RawQuery($"REMOVE DATABASE `{_databaseInfo.Database}`;");
        }

        Dispose();
    }
}
