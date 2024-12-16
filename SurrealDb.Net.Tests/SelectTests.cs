using System.Text;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests;

public class Empty : SurrealDbRecord { }

public class Post : SurrealDbRecord
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }

    [CborIgnoreIfDefault]
    public string? Status { get; set; }
}

public class ObjectTableId
{
    [CborProperty("location")]
    public string Location { get; set; } = string.Empty;

    [CborProperty("year")]
    public int Year { get; set; }
}

public class SelectTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectFromEmptyTable(string connectionString)
    {
        IEnumerable<Empty>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.Select<Empty>("empty");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectFromPostTable(string connectionString)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/post.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            result = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(2);

        var list = result!.ToList();

        var firstPost = list.FirstOrDefault(p => p.Id! == ("post", "first"));

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");

        var secondPost = list.First(p => p != firstPost);

        secondPost.Should().NotBeNull();
        secondPost!.Title.Should().Be("Second article");
        secondPost!.Content.Should().Be("Another article");
        secondPost!.CreatedAt.Should().NotBeNull();
        secondPost!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSinglePostUsingTwoArguments(string connectionString)
    {
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/post.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            result = await client.Select<Post>(("post", "first"));
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSinglePostUsingRecordId(string connectionString)
    {
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/post.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = new RecordIdOfString("post", "first");

            result = await client.Select<Post>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSingleFromNumberId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = RecordId.From("recordId", 17493);

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("number");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSingleFromStringId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = RecordId.From("recordId", "surrealdb");

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("string");
    }

    [Theory(Skip = "Guid not currently handled")]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSingleFromGuidId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = RecordId.From(
                "recordId",
                new Guid("8424486b-85b3-4448-ac8d-5d51083391c7")
            );

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("complex");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSingleFromObjectId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = RecordId.From(
                "recordId",
                new ObjectTableId { Location = "London", Year = 2023 }
            );

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("object");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSingleFromArrayId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = RecordId.From("recordId", new object[] { "London", 2023 });

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("array");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectSingleFromStringRecordIdType(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            var recordId = new StringRecordId("recordId:surrealdb");

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("string");
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSelectFromRecordIdRange(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Empty>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            var itemsToInsert = new List<Empty>(26);

            foreach (char c in alphabet)
            {
                itemsToInsert.Add(new Empty { Id = ("empty", c.ToString()) });
            }

            await client.Insert("empty", itemsToInsert);

            result = await client.Select<string, string, Empty>(
                new RecordIdRange<string, string>(
                    "empty",
                    RangeBound.Inclusive("b"),
                    RangeBound.Exclusive("d")
                )
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        result
            .Should()
            .BeEquivalentTo([new Empty { Id = ("empty", "b") }, new Empty { Id = ("empty", "c") }]);
    }
}
