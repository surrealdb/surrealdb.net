using System.Text.Json.Serialization;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
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

public class SurrealDbClientGenerator : IDisposable, IAsyncDisposable
{
    private static readonly DatabaseInfoFaker _databaseInfoFaker = new();

    private ServiceProvider? _serviceProvider;
    private DatabaseInfo? _databaseInfo;

    public SurrealDbClient Create(
        string endpoint,
        Func<JsonSerializerContext[]>? funcJsonSerializerContexts = null
    )
    {
        var services = new ServiceCollection();

        var options = SurrealDbOptions.Create().WithEndpoint(endpoint).Build();

        services.AddSurreal(options, appendJsonSerializerContexts: funcJsonSerializerContexts);

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);

        return _serviceProvider.GetRequiredService<SurrealDbClient>();
    }

    public DatabaseInfo GenerateDatabaseInfo()
    {
        _databaseInfo = _databaseInfoFaker.Generate();
        return _databaseInfo;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_databaseInfo is not null)
        {
            using var client = new SurrealDbClient(
                "http://127.0.0.1:8000",
                _serviceProvider!.GetRequiredService<IHttpClientFactory>()
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(_databaseInfo.Namespace, _databaseInfo.Database);

            string query = $"REMOVE DATABASE {_databaseInfo.Database};";
            await client.Query(query);
        }

        Dispose();
    }
}
