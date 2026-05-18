namespace SurrealDb.Net.Tests.Queryable;

public class ExplainQueryableTests : BaseQueryableTests
{
    private readonly VerifySettings _verifySettings = new();

    public ExplainQueryableTests()
    {
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.IgnoreParameters();
        _verifySettings.ScrubMember<ExplainMetrics>(m => m.ElapsedNs);
    }

    [Test]
    public void Explain()
    {
        string query = ToSurql(Users.Select(u => u.Age).Explain());

        query
            .Should()
            .Be(
                """
                SELECT * FROM (SELECT VALUE Age FROM user EXPLAIN)
                """
            );
    }

    [Test]
    public void ExplainFull()
    {
        string query = ToSurql(Users.Select(u => u.Age).Explain(true));

        query
            .Should()
            .Be(
                """
                SELECT * FROM (SELECT VALUE Age FROM user EXPLAIN FULL)
                """
            );
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ExplainQuery(string connectionString)
    {
        var (result, query) = await ExecuteWithSchemaAsync(
            connectionString,
            SurrealSchemaFile.Bearer,
            client => client.Select<User>().Explain()
        );

        query
            .Should()
            .Be(
                "SELECT * FROM (SELECT avatar, email, id, password, registered_at, username FROM user EXPLAIN)"
            );

        await Verify(result, _verifySettings);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ExplainQueryWithIndex(string connectionString)
    {
        var (result, query) = await ExecuteWithSchemaAsync(
            connectionString,
            SurrealSchemaFile.Bearer,
            client => client.Select<User>().Where(user => user.Username == "admin").Explain()
        );

        query
            .Should()
            .Be(
                """
                SELECT * FROM (SELECT avatar, email, id, password, registered_at, username FROM user WHERE username == "admin" EXPLAIN)
                """
            );

        await Verify(result, _verifySettings);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ExplainQueryFull(string connectionString)
    {
        var (result, query) = await ExecuteWithSchemaAsync(
            connectionString,
            SurrealSchemaFile.Bearer,
            client => client.Select<User>().Explain(true)
        );

        query
            .Should()
            .Be(
                "SELECT * FROM (SELECT avatar, email, id, password, registered_at, username FROM user EXPLAIN FULL)"
            );

        await Verify(result, _verifySettings);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ExplainQueryFullWithIndex(string connectionString)
    {
        var (result, query) = await ExecuteWithSchemaAsync(
            connectionString,
            SurrealSchemaFile.Bearer,
            client => client.Select<User>().Where(user => user.Username == "admin").Explain(true)
        );

        query
            .Should()
            .Be(
                """
                SELECT * FROM (SELECT avatar, email, id, password, registered_at, username FROM user WHERE username == "admin" EXPLAIN FULL)
                """
            );

        await Verify(result, _verifySettings);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ConsumeExplainQuery(string connectionString)
    {
        var (result, query) = await ExecuteWithSchemaAsync(
            connectionString,
            SurrealSchemaFile.Bearer,
            client => client.Select<User>().Explain().Select(p => p.Operator)
        );

        query
            .Should()
            .Be(
                "SELECT VALUE operator FROM (SELECT avatar, email, id, password, registered_at, username FROM user EXPLAIN)"
            );
        result.Should().BeEquivalentTo(["SelectProject"]);
    }
}
