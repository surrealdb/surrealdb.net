using System.Text;

namespace SurrealDb.Net.Tests;

public class AuthParams : ScopeAuth
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SignInTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSignInAsRootUser(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        };

        await func.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    public async Task SignInAsRootUserIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSignInUsingNamespaceAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON NAMESPACE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            jwt = await client.SignIn(
                new NamespaceAuth
                {
                    Namespace = dbInfo.Namespace,
                    Username = "johndoe",
                    Password = "password123"
                }
            );
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Value.Token.Should().BeValidJwt();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
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
                    Password = "password123"
                }
            );
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSignInUsingDatabaseAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON DATABASE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            jwt = await client.SignIn(
                new DatabaseAuth
                {
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                    Username = "johndoe",
                    Password = "password123"
                }
            );
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Value.Token.Should().BeValidJwt();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    public async Task SignInUsingDatabaseAuthIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON DATABASE PASSWORD 'password123'";
            (await client.RawQuery(query)).EnsureAllOks();

            await client.SignIn(
                new DatabaseAuth
                {
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                    Username = "johndoe",
                    Password = "password123"
                }
            );
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSignInUsingScopeAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/user.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;
            (await client.RawQuery(query)).EnsureAllOks();

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123"
            };
#pragma warning restore CS0618 // Type or member is obsolete

            await client.SignUp(authParams);

            jwt = await client.SignIn(authParams);
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Value.Token.Should().BeValidJwt();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    public async Task SignInUsingScopeAuthIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/user.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;
            (await client.RawQuery(query)).EnsureAllOks();

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123"
            };
#pragma warning restore CS0618 // Type or member is obsolete

            await client.SignUp(authParams);

            await client.SignIn(authParams);
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }
}
