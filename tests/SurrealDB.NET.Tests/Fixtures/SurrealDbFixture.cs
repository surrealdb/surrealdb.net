using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SurrealDB.NET.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class SurrealDbCollectionFixture
	: ICollectionFixture<SurrealDbFixture>
{
	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.

	public const string Name = "SurrealDB";
}

public sealed class SurrealDbFixture : IAsyncLifetime
{
    private readonly IContainer _container;

    public ushort Port => _container.GetMappedPublicPort(8000);

    public SurrealDbFixture()
    {
        _container = new ContainerBuilder()
            .WithPortBinding(8000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(http => http
                .WithMethod(HttpMethod.Get)
                .ForStatusCode(System.Net.HttpStatusCode.OK)
                .ForPath("/health")
                .ForPort(8000)))
            .WithImage("surrealdb/surrealdb:1.0.0")
            .WithCommand("start", "--auth", "-u", "root", "-p", "root", "-A")
            .Build();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync().ConfigureAwait(false);
        await _container.DisposeAsync().ConfigureAwait(false);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        using var di = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{SurrealOptions.Section}:Endpoint"] = $"ws://localhost:{Port}/rpc",
                    [$"{SurrealOptions.Section}:DefaultNamespace"] = "test",
                    [$"{SurrealOptions.Section}:DefaultDatabase"] = "test",
                }).Build())
            .AddSurrealDB()
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

		using var rootScope = di.CreateScope();
        var root = rootScope.ServiceProvider.GetRequiredKeyedService<ISurrealRpcClient>("text");
        await root.SigninRootAsync("root", "root").ConfigureAwait(false);

        _ = await root.QueryAsync("""
            DEFINE TABLE user SCHEMAFULL;
            DEFINE FIELD email ON user TYPE string;
            DEFINE FIELD password ON user TYPE string;
            DEFINE INDEX index_user_email ON user COLUMNS email UNIQUE;
            DEFINE TABLE post SCHEMAFULL PERMISSIONS FULL;
            DEFINE FIELD content ON post TYPE string;
            DEFINE FIELD tags ON post TYPE option<string> DEFAULT NONE;
            """).ConfigureAwait(false);

        _ = await root.QueryAsync("""
            DEFINE SCOPE account SESSION 24h
                SIGNUP (CREATE user SET email = $email, password = crypto::argon2::generate($password))
                SIGNIN (SELECT * FROM user WHERE email = $email AND crypto::argon2::compare(password, $password))
            """).ConfigureAwait(false);

		_ = await root.QueryAsync("""
			USE NS test DB test;
			DEFINE USER test_root ON NAMESPACE PASSWORD "test_root";
			""");
    }
}
