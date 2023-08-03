using SurrealDb.Internals.Models;
using SurrealDb.Models.Response;
using System.Net;
using System.Text;

namespace SurrealDb.Tests;

public class QueryTests
{
	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldQueryWithParams(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = "SELECT * FROM post WHERE status == $status;";

				response = await client.Query(query, new Dictionary<string, object> { { "status", "DRAFT" } });
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
	public async Task ShouldHaveOneProtocolErrorResult(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = "SELECT;";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull().And.HaveCount(1);

		var firstResult = response![0];
		firstResult.Should().BeOfType<SurrealDbProtocolErrorResult>();

		var errorResult = firstResult as SurrealDbProtocolErrorResult;

		errorResult!.Code.Should().Be(HttpStatusCode.BadRequest);
		errorResult!.Details.Should().Be("Request problems detected");
		errorResult!.Description.Should().Be("There is a problem with your request. Refer to the documentation for further information.");
		errorResult!.Information.Should().Be("There was a problem with the database: Parse error on line 1 at character 0 when parsing 'SELECT;'");
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldHave4Results(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;
SELECT xyz FROM post;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		response!.Count.Should().Be(4);
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldIterateOnOkResults(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		response!.Oks.Should().NotBeNull().And.HaveCount(3);
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldIterateOnErrorResults(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		response!.Errors.Should().NotBeNull().And.HaveCount(1);
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldReturnFirstOkResult(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		response!.FirstOk.Should().BeOfType<SurrealDbOkResult>();
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldReturnFirstError(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		response!.FirstError.Should().BeOfType<SurrealDbErrorResult>();
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldHaveError(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		response!.HasErrors.Should().BeTrue();
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldGetValueFromIndex(string url)
	{
		SurrealDbResponse? response = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			var client = surrealDbClientGenerator.Create(url);
			await client.SignIn(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);

			{
				string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

				string query = fileContent;

				await client.Query(query);
			}

			{
				string query = @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

				response = await client.Query(query);
			}
		};

		await func.Should().NotThrowAsync();

		response.Should().NotBeNull();
		var list = response!.GetValue<List<Post>>(0);

		list.Should().NotBeNull().And.HaveCount(2);
	}
}
