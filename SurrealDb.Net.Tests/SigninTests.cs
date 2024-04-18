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
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
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
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldSignInUsingNamespaceAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON NAMESPACE PASSWORD 'password123'";
            await client.RawQuery(query);

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
        jwt!.Token.Should().BeValidJwt();
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldSignInUsingDatabaseAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string query = "DEFINE USER johndoe ON DATABASE PASSWORD 'password123'";
            await client.RawQuery(query);

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
        jwt!.Token.Should().BeValidJwt();
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldSignInUsingScopeAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/user.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;
            await client.RawQuery(query);

            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123"
            };

            await client.SignUp(authParams);

            jwt = await client.SignIn(authParams);
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Token.Should().BeValidJwt();
    }
}
