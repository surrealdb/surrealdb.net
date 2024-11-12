using System.Text;

namespace SurrealDb.Net.Tests;

public class EmptyRelation : SurrealDbRelationRecord { }

public class WroteRelation : SurrealDbRelationRecord
{
    public DateTime CreatedAt { get; set; }
    public int NumberOfPages { get; set; }
}

public class RelateTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreateEmptyRelation(string connectionString)
    {
        IEnumerable<EmptyRelation>? list = null;
        EmptyRelation? result = null;

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

            result = await client.Relate<EmptyRelation>("empty", ("in", "one"), ("out", "one"));

            list = await client.Select<EmptyRelation>("empty");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.In.Should().Be(new RecordIdOfString("in", "one"));
        result!.Out.Should().Be(new RecordIdOfString("out", "one"));
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreateWroteRelation(string connectionString)
    {
        IEnumerable<WroteRelation>? list = null;
        WroteRelation? result = null;

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

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            result = await client.Relate<WroteRelation, WroteRelation>(
                "wrote",
                ("user", "one"),
                ("post", "one"),
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.In.Should().Be(new RecordIdOfString("user", "one"));
        result!.Out.Should().Be(new RecordIdOfString("post", "one"));
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldCreateWroteRelationWithPredefinedId(string connectionString)
    {
        IEnumerable<WroteRelation>? list = null;
        WroteRelation? result = null;

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

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            result = await client.Relate<WroteRelation, WroteRelation>(
                ("wrote", "one"),
                ("user", "one"),
                ("post", "one"),
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);

        var relationInList = list!.First(r => r.Id!.DeserializeId<string>() == "one");

        relationInList.Should().NotBeNull();
        relationInList!.In.Should().Be(new RecordIdOfString("user", "one"));
        relationInList!.Out.Should().Be(new RecordIdOfString("post", "one"));
        relationInList!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        relationInList!.NumberOfPages.Should().Be(14);
    }
}
