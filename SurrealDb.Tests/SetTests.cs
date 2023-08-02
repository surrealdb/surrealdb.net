using SurrealDb.Internals.Models;
using SurrealDb.Models.Response;
using System.Text;

namespace SurrealDb.Tests;

public class SetTests
{
	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldSetParam(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			await client.Set("status", "DRAFT");

			{
				string query = "SELECT * FROM post WHERE status == $status;";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull().And.HaveCount(1);

		var firstResult = response![0];
		firstResult.Should().BeOfType<SurrealDbOkResult>();

		var okResult = firstResult as SurrealDbOkResult;
		var list = okResult!.GetValue<List<Post>>();

		list.Should().NotBeNull().And.HaveCount(2);
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldUnsetParam(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			await client.Set("status", "DRAFT");
			client.Unset("status");

			{
				string query = "SELECT * FROM post WHERE status == $status;";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull().And.HaveCount(1);

		var firstResult = response![0];
		firstResult.Should().BeOfType<SurrealDbOkResult>();

		var okResult = firstResult as SurrealDbOkResult;
		var list = okResult!.GetValue<List<Post>>();

		list.Should().BeEmpty();
	}
}
