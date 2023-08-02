using System.Text;

namespace SurrealDb.Tests;

public class AuthParams : ScopeAuth
{
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

public class SigninTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
    public async Task ShouldSigninAsRootUser(string url)
    {
        Func<Task> func = async () => 
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

            var client = surrealDbClientGenerator.Create(url);
            await client.Signin(new RootAuth { Username = "root", Password = "root" });
        };

        await func.Should().NotThrowAsync();
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldSigninUsingNamespaceAuth(string url)
	{
		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			string query = "DEFINE LOGIN johndoe ON NAMESPACE PASSWORD 'password123'";
			await client.Query(query);

			await client.Signin(
				new NamespaceAuth { Namespace = dbInfo.Namespace, Username = "johndoe", Password = "password123" }
			);
		};

		await func.Should().NotThrowAsync();
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldSigninUsingDatabaseAuth(string url)
	{
		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			string query = "DEFINE LOGIN johndoe ON DATABASE PASSWORD 'password123'";
			await client.Query(query);

			await client.Signin(
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
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldSigninUsingScopeAuth(string url)
	{
		Jwt? jwt = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/user.surql");
			string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

			string query = fileContent;
			await client.Query(query);

			var authParams = new AuthParams
			{
				Namespace = dbInfo.Namespace,
				Database = dbInfo.Database,
				Scope = "user_scope",
				Username = "johndoe",
				Email = "john.doe@example.com",
				Password = "password123"
			};

			await client.Signup(authParams);

			jwt = await client.Signin(authParams);
		};

		await func.Should().NotThrowAsync();

		jwt.Should().NotBeNull();
		jwt!.Token.Should().BeValidJwt();
	}
}
