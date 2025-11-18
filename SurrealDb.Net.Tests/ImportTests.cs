namespace SurrealDb.Net.Tests;

public class ImportTests
{
    private const string IMPORT_QUERY = """
        DEFINE TABLE foo SCHEMALESS;
        DEFINE TABLE bar SCHEMALESS;
        CREATE foo:1 CONTENT { hello: "world" };
        CREATE bar:1 CONTENT { hello: "world" };
        DEFINE FUNCTION fn::foo() {
          RETURN "bar";
        };
        """;

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldImportDataSuccessfully(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);
        if (version?.Major < 2)
        {
            return;
        }

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        Func<Task> func = async () =>
        {
            await client.Import(IMPORT_QUERY);
        };

        await func.Should().NotThrowAsync();

        // Check imported query by querying the db
        var fooRecords = await client.Select<object>("foo").ToListAsync();
        fooRecords.Should().NotBeNull().And.HaveCount(1);

        var barRecords = await client.Select<object>("bar").ToListAsync();
        barRecords.Should().NotBeNull().And.HaveCount(1);

        var fnResult = await client.Run<string>("fn::foo");
        fnResult.Should().Be("bar");

        await client.DisposeAsync();
    }
}
