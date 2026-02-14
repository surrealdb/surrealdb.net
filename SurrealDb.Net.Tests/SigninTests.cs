using System.ComponentModel.DataAnnotations.Schema;

namespace SurrealDb.Net.Tests;

public class AuthParams : ScopeAuth
{
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;
}

public class AccessResponse
{
    [Column("grant")]
    public AccessResponseGrant Grant { get; set; } = null!;
}

public class AccessResponseGrant
{
    [Column("key")]
    public string Key { get; set; } = string.Empty;
}

public class AcessAuthParams : ScopeAuth
{
    [Column("key")]
    public required string Key { get; init; }
}

public class SignInTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSignInAsRootUser(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        };

        await func.Should().NotThrowAsync();
    }

    [Test]
    [EmbeddedConnectionStringFixtureGenerator]
    public async Task SignInAsRootUserIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSignInUsingNamespaceAuth(string connectionString)
    {
        Tokens? tokens = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON NAMESPACE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            tokens = await client.SignIn(
                new NamespaceAuth
                {
                    Namespace = dbInfo.Namespace,
                    Username = "johndoe",
                    Password = "password123",
                }
            );
        };

        await func.Should().NotThrowAsync();

        tokens.Should().NotBeNull();
        tokens!.Access.Should().BeValidJwt();
    }

    [Test]
    [EmbeddedConnectionStringFixtureGenerator]
    public async Task SignInUsingNamespaceAuthIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON NAMESPACE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            await client.SignIn(
                new NamespaceAuth
                {
                    Namespace = dbInfo.Namespace,
                    Username = "johndoe",
                    Password = "password123",
                }
            );
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSignInUsingDatabaseAuth(string connectionString)
    {
        Tokens? tokens = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON DATABASE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            tokens = await client.SignIn(
                new DatabaseAuth
                {
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                    Username = "johndoe",
                    Password = "password123",
                }
            );
        };

        await func.Should().NotThrowAsync();

        tokens.Should().NotBeNull();
        tokens!.Access.Should().BeValidJwt();
    }

    [Test]
    [EmbeddedConnectionStringFixtureGenerator]
    public async Task SignInUsingDatabaseAuthIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON DATABASE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            await client.SignIn(
                new DatabaseAuth
                {
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                    Username = "johndoe",
                    Password = "password123",
                }
            );
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSignInUsingScopeAuth(string connectionString)
    {
        Tokens? tokens = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123",
            };
#pragma warning restore CS0618 // Type or member is obsolete

            await client.SignUp(authParams);

            tokens = await client.SignIn(authParams);
        };

        await func.Should().NotThrowAsync();

        tokens.Should().NotBeNull();
        tokens!.Access.Should().BeValidJwt();
    }

    [Test]
    [EmbeddedConnectionStringFixtureGenerator]
    public async Task SignInUsingScopeAuthIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123",
            };
#pragma warning restore CS0618 // Type or member is obsolete

            await client.SignUp(authParams);

            await client.SignIn(authParams);
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldSignInUsingBearerAuth(string connectionString)
    {
        Tokens? tokens = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Bearer);

            var response = await client.RawQuery("ACCESS api GRANT FOR RECORD user:johndoe;");
            response.EnsureAllOks();

            var access = response.FirstOk!.GetValue<AccessResponse>()!;

            var authParams = new AcessAuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Access = "api",
                Key = access.Grant.Key,
            };

            tokens = await client.SignIn(authParams);
        };

        await func.Should().NotThrowAsync();

        tokens.Should().NotBeNull();
        tokens!.Access.Should().BeValidJwt();
    }
}
