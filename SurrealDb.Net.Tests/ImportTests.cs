using System.Net;
using Semver;
using SurrealDb.Net.Exceptions.Methods;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public class ImportTests
{
    private const string OPTION_IMPORT = "OPTION IMPORT;";
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

        bool shouldPrefixOptionImport =
            version is not null && version.Satisfies(SemVersionRange.AtLeast(new(3, 0, 4), true));

        string importQuery = shouldPrefixOptionImport
            ? $"{OPTION_IMPORT}\n{IMPORT_QUERY}"
            : IMPORT_QUERY;

        Func<Task> func = async () =>
        {
            await client.Import(importQuery);
        };

        await func.Should().NotThrowAsync();

        // Check imported query by querying the db
        var fooRecords = await client.Select<object>("foo");
        fooRecords.Should().NotBeNull().And.HaveCount(1);

        var barRecords = await client.Select<object>("bar");
        barRecords.Should().NotBeNull().And.HaveCount(1);

        var fnResult = await client.Run<string>("fn::foo");
        fnResult.Should().Be("bar");

        await client.DisposeAsync();
    }

    private static async Task<string> PrefixImportAsync(string connectionString, string query)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);
        bool shouldPrefixOptionImport =
            version is not null && version.Satisfies(SemVersionRange.AtLeast(new(3, 0, 4), true));

        return shouldPrefixOptionImport ? $"{OPTION_IMPORT}\n{query}" : query;
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.0")]
    public async Task ShouldReportFailedStatements(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        string importQuery = await PrefixImportAsync(
            connectionString,
            """
            DEFINE TABLE foo SCHEMALESS;
            CREATE foo:1 CONTENT { hello: "world" };
            THROW "boom";
            CREATE foo:1 CONTENT { hello: "again" };
            CREATE foo:2 CONTENT { hello: "world" };
            """
        );

        Func<Task> func = () => client.Import(importQuery);

        var exception = (await func.Should().ThrowAsync<SurrealDbImportException>()).Which;
        exception.Errors.Should().HaveCount(2);
        exception
            .Errors.OfType<SurrealDbErrorResult>()
            .Select(e => e.Details)
            .Should()
            .Contain(d => d.Contains("boom"))
            .And.Contain(d => d.Contains("foo:1"));
        exception.Message.Should().StartWith("2 statements failed during the import:");

        // 💡 The other statements were imported
        var records = await client.Select<object>("foo");
        records.Should().HaveCount(2);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.0")]
    public async Task ShouldReportParseError(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        string importQuery = await PrefixImportAsync(connectionString, "CREATE foo:1 SET x = ;");

        Func<Task> func = () => client.Import(importQuery);

        var exception = (await func.Should().ThrowAsync<SurrealDbImportException>()).Which;
        exception.Errors.Should().ContainSingle();
        exception.Message.Should().Contain("Parse error");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0.4")]
    public async Task ShouldReportMissingOptionImport(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        Func<Task> func = () => client.Import("CREATE foo:1;");

        var exception = (await func.Should().ThrowAsync<SurrealDbImportException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Errors.Should().BeEmpty();
        exception.Message.Should().Contain("OPTION IMPORT");
    }
}
