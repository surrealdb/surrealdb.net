using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Tests.Fixtures;

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
    private SurrealDbClient? _client;
    private DatabaseInfo? _databaseInfo;

    public SurrealDbClient Create(string endpoint)
    {
        var services = new ServiceCollection();

		var options = SurrealDbOptions
			.Create()
			.WithEndpoint(endpoint)
			.Build();

		services.AddSurreal(options);

		_serviceProvider = services.BuildServiceProvider(validateScopes: true);
        using var scope = _serviceProvider.CreateScope();

        _client = scope.ServiceProvider.GetRequiredService<SurrealDbClient>();
        return _client;
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
		if (_databaseInfo is not null && _client is not null)
		{
			string query = $"REMOVE DATABASE {_databaseInfo.Database};";
			await _client.Query(query);
		}

		Dispose();
	}
}
