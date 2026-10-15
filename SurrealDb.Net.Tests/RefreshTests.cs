using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests;

public class AccountAuthParams : ScopeAuth
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("pass")]
    public string Pass { get; set; } = string.Empty;
}

public class RefreshTests
{
    private static readonly TimeSpan TokenDuration = TimeSpan.FromSeconds(2);

    private static async Task<(SurrealDbClient Client, Tokens Tokens)> SignUpAsync(
        SurrealDbClientGenerator surrealDbClientGenerator,
        string connectionString,
        Action<SurrealDbOptions>? configure = null
    )
    {
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using (var rootClient = surrealDbClientGenerator.Create(connectionString))
        {
            await rootClient.Use(dbInfo.Namespace, dbInfo.Database);
            (
                await rootClient.RawQuery(
                    $"""
                    DEFINE TABLE user SCHEMALESS PERMISSIONS FOR select WHERE id = $auth.id;
                    DEFINE ACCESS account ON DATABASE TYPE RECORD
                        SIGNUP (CREATE user SET name = $name, pass = crypto::argon2::generate($pass))
                        SIGNIN (SELECT * FROM user WHERE name = $name AND crypto::argon2::compare(pass, $pass))
                        WITH REFRESH
                        DURATION FOR TOKEN {(int)TokenDuration.TotalSeconds}s, FOR SESSION 1h;
                    """
                )
            ).EnsureAllOks();
        }

        // 💡 A client with no root credentials, so that only the record user is authenticated
        var rootOptions = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        var options = new SurrealDbOptions(rootOptions.Endpoint!);
        configure?.Invoke(options);

        var client = new SurrealDbClient(options);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        var tokens = await client.SignUp(
            new AccountAuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Access = "account",
                Name = "john",
                Pass = "secret",
            }
        );

        return (client, tokens);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldRefreshTokens(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var (client, tokens) = await SignUpAsync(surrealDbClientGenerator, connectionString);
        await using var _ = client;

        tokens.Refresh.Should().NotBeNullOrEmpty();

        var newTokens = await client.Refresh(tokens);

        newTokens.Access.Should().BeValidJwt();
        newTokens.Refresh.Should().NotBeNullOrEmpty().And.NotBe(tokens.Refresh);

        var response = await client.RawQuery("RETURN <string> $auth.id;");
        response.GetValue<string>(0).Should().StartWith("user:");

        // 💡 A refresh token can only be used once
        Func<Task> refreshAgain = () => client.Refresh(tokens);
        await refreshAgain.Should().ThrowAsync<SurrealDbException>();
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldRevokeRefreshToken(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var (client, tokens) = await SignUpAsync(surrealDbClientGenerator, connectionString);
        await using var _ = client;

        await client.Revoke(tokens);

        Func<Task> refresh = () => client.Refresh(tokens);
        await refresh.Should().ThrowAsync<SurrealDbException>();
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldRequireRefreshToken(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        Func<Task> refresh = () => client.Refresh(new Tokens("access"));
        await refresh.Should().ThrowAsync<ArgumentException>();

        Func<Task> revoke = () => client.Revoke(new Tokens("access"));
        await revoke.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldRenewExpiredAccessToken(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var (client, _) = await SignUpAsync(surrealDbClientGenerator, connectionString);
        await using var __ = client;

        await Task.Delay(TokenDuration + TimeSpan.FromSeconds(1));

        if (client.Engine is SurrealDbWsEngine wsEngine)
        {
            // 💡 Over WebSocket, an expired token matters when the session is restored
            await wsEngine.ForceReconnectAsync();
        }

        var response = await client.RawQuery("RETURN <string> $auth.id;");
        response.GetValue<string>(0).Should().StartWith("user:");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldInvalidateOnExpiry(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var (client, _) = await SignUpAsync(
            surrealDbClientGenerator,
            connectionString,
            options => options.InvalidateOnExpiry = true
        );
        await using var __ = client;

        await Task.Delay(TokenDuration + TimeSpan.FromSeconds(1));

        var response = await client.RawQuery("RETURN $auth;");
        response.GetValue<object?>(0).Should().BeNull();
    }
}
