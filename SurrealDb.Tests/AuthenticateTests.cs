using System.Text;

namespace SurrealDb.Tests;

public class AuthenticateTests
{
	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldAuthenticate(string url)
	{
		Jwt? jwt = null;
		List<Post>? list = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/user.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;
				await client.Query(query);
			}

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;
				await client.Query(query);
			}

			var authParams = new AuthParams
			{
				Namespace = dbInfo.Namespace,
				Database = dbInfo.Database,
				Scope = "user_scope",
				Username = "johndoe",
				Email = "john.doe@example.com",
				Password = "password123"
			};

			jwt = await client.SignUp(authParams);

			await client.Authenticate(jwt);

			list = await client.Select<Post>("post");
		};

		await func.Should().NotThrowAsync();

		list.Should().NotBeNull().And.HaveCount(2);
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldFailWhenInvalidate(string url)
	{
		Jwt? jwt = null;
		List<Post>? list = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/user.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;
				await client.Query(query);
			}

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;
				await client.Query(query);
			}

			var authParams = new AuthParams
			{
				Namespace = dbInfo.Namespace,
				Database = dbInfo.Database,
				Scope = "user_scope",
				Username = "johndoe",
				Email = "john.doe@example.com",
				Password = "password123"
			};

			jwt = await client.SignUp(authParams);

			client.Invalidate();

			list = await client.Select<Post>("post");
		};

		await func.Should().NotThrowAsync();

		list.Should().NotBeNull().And.HaveCount(0);
	}
}
