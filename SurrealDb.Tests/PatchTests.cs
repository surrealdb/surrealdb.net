using System.Text;

namespace SurrealDb.Tests;

public class PostPatch : SurrealDbRecord
{
	public string Content { get; set; } = string.Empty;
}

public class PatchTests
{
	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldPatchExistingPost(string url)
	{
		List<Post>? list = null;
		Post? result = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
			string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

			string query = fileContent;

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);
			await client.Query(query);

			var patch = new PostPatch
			{
				Id = new Thing("post", "first"),
				Content = "[Edit] This is my first article"
			};

			result = await client.Patch<PostPatch, Post>(patch);

			list = await client.Select<Post>("post");
		};

		await func.Should().NotThrowAsync();

		list.Should().NotBeNull().And.HaveCount(2);

		result.Should().NotBeNull();
		result!.Title.Should().Be("First article");
		result!.Content.Should().Be("[Edit] This is my first article");
		result!.CreatedAt.Should().NotBeNull();
		result!.Status.Should().Be("DRAFT");
	}

	[Theory]
	[InlineData("http://localhost:8000")]
	[InlineData("ws://localhost:8000/rpc", Skip = "NotImplemented")]
	public async Task ShouldPatchUsingDictionary(string url)
	{
		List<Post>? list = null;
		Post? result = null;

		Func<Task> func = async () =>
		{
			await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
			var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
			string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

			string query = fileContent;

			var client = surrealDbClientGenerator.Create(url);
			await client.Signin(new RootAuth { Username = "root", Password = "root" });
			await client.Use(dbInfo.Namespace, dbInfo.Database);
			await client.Query(query);

			var thing = new Thing("post", "first");
			var data = new Dictionary<string, object>
			{
				{ "content", "[Edit] This is my first article" }
			};

			result = await client.Patch<Post>(thing, data);

			list = await client.Select<Post>("post");
		};

		await func.Should().NotThrowAsync();

		list.Should().NotBeNull().And.HaveCount(2);

		result.Should().NotBeNull();
		result!.Title.Should().Be("First article");
		result!.Content.Should().Be("[Edit] This is my first article");
		result!.CreatedAt.Should().NotBeNull();
		result!.Status.Should().Be("DRAFT");
	}
}
