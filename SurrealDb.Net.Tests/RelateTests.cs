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
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldCreateEmptyRelation(string url)
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

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            result = await client.Relate<EmptyRelation>(
                "empty",
                new Thing("in", "one"),
                new Thing("out", "one")
            );

            list = await client.Select<EmptyRelation>("empty");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.In.Should().Be(new Thing("in", "one"));
        result!.Out.Should().Be(new Thing("out", "one"));
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldCreateWroteRelation(string url)
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

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            result = await client.Relate<WroteRelation, WroteRelation>(
                "wrote",
                new Thing("user", "one"),
                new Thing("post", "one"),
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.In.Should().Be(new Thing("user", "one"));
        result!.Out.Should().Be(new Thing("post", "one"));
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldCreateWroteRelationWithPredefinedId(string url)
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

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.Query(query);

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            result = await client.Relate<WroteRelation, WroteRelation>(
                new Thing("wrote", "one"),
                new Thing("user", "one"),
                new Thing("post", "one"),
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);

        var relationInList = list!.First(r => r.Id!.Id == "one");

        relationInList.Should().NotBeNull();
        relationInList!.In.Should().Be(new Thing("user", "one"));
        relationInList!.Out.Should().Be(new Thing("post", "one"));
        relationInList!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        relationInList!.NumberOfPages.Should().Be(14);
    }
}
