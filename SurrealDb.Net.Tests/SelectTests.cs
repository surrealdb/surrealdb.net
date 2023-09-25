using System.Text;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Tests;

public class Empty : SurrealDbRecord { }
public class Post : SurrealDbRecord
{
    public string Title { get; set; } = string.Empty;

	public string Content { get; set; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? CreatedAt { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Status { get; set; }
}

public class ObjectTableId
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

	[JsonPropertyName("year")]
    public int Year { get; set; }
}

public class SelectTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectFromEmptyTable(string url)
    {
        List<Empty>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.Select<Empty>("empty");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectFromPostTable(string url)
    {
        List<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            result = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(2);

        var firstPost = result!.Find(p => p.Id!.Id == "first");

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");

        var secondPost = result!.Find(p => p != firstPost);

        secondPost.Should().NotBeNull();
        secondPost!.Title.Should().Be("Second article");
        secondPost!.Content.Should().Be("Another article");
        secondPost!.CreatedAt.Should().NotBeNull();
        secondPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSinglePostUsingTwoArguments(string url)
    {
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            result = await client.Select<Post>("post", "first");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSinglePostUsingThing(string url)
    {
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/post.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = new Thing("post", "first");

            result = await client.Select<Post>(thing);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSingleFromNumberId(string url)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/thing.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = Thing.From("thing", 17493);

            result = await client.Select<RecordIdRecord>(thing);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("number");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSingleFromStringId(string url)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/thing.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = Thing.From("thing", "surrealdb");

            result = await client.Select<RecordIdRecord>(thing);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("string");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSingleFromGuidId(string url)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/thing.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = Thing.From("thing", new Guid("8424486b-85b3-4448-ac8d-5d51083391c7"));

            result = await client.Select<RecordIdRecord>(thing);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("complex");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSingleFromObjectId(string url)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/thing.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = Thing.From("thing", new ObjectTableId { Location = "London", Year = 2023 });

            result = await client.Select<RecordIdRecord>(thing);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("object");
    }

    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSelectSingleFromArrayId(string url)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas/thing.surql");
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

			using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var thing = Thing.From("thing", new List<object> { "London", 2023 });

            result = await client.Select<RecordIdRecord>(thing);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("array");
    }
}
