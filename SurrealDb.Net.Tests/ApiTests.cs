using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Api;

namespace SurrealDb.Net.Tests;

public class ApiTests
{
    private const string DEFINE_APIS = """
        DEFINE API "/hello" FOR get THEN {
            RETURN { status: 200, body: { hello: $request.query.name ?? "world" } };
        };
        DEFINE API "/users/echo" FOR post, put THEN {
            RETURN {
                status: 201,
                body: { got: $request.body, header: $request.headers["x-test"] },
                headers: { "x-answer": "42" },
            };
        };
        DEFINE API "/users" FOR get THEN {
            RETURN { status: 200, body: { root: true } };
        };
        DEFINE API "/body" FOR post THEN {
            RETURN { status: 200, body: { none: $request.body == NONE, null: $request.body == NULL } };
        };
        """;

    private static async Task<SurrealDbClient> CreateClientAsync(
        SurrealDbClientGenerator surrealDbClientGenerator,
        string connectionString
    )
    {
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        (await client.RawQuery(DEFINE_APIS)).EnsureAllOks();

        return client;
    }

    public class EchoBody
    {
        [Column("got")]
        public Dictionary<string, int> Got { get; set; } = new();

        [Column("header")]
        public string? Header { get; set; }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldInvokeGet(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        var response = await client.Api().Get("/hello");

        response.Status.Should().Be(200);
        response.IsSuccessStatusCode.Should().BeTrue();
        response.GetBody<Dictionary<string, string>>().Should().Contain("hello", "world");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldSendQueryHeadersAndBody(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        var hello = await client
            .Api()
            .Invoke(
                "hello",
                new SurrealDbApiRequest
                {
                    Query = new Dictionary<string, string> { ["name"] = "bob" },
                }
            );
        hello.GetBody<Dictionary<string, string>>().Should().Contain("hello", "bob");

        var echo = await client
            .Api("/users")
            .Header("x-test", "yes")
            .Post("/echo", new Dictionary<string, object?> { ["a"] = 1 });

        echo.Status.Should().Be(201);
        echo.Method.Should().Be("post");
        echo.Path.Should().Be("/users/echo");
        echo.Headers.Should().Contain("x-answer", "42");
        echo.Headers["X-Answer"].Should().Be("42");

        var body = echo.GetBody<EchoBody>()!;
        body.Header.Should().Be("yes");
        body.Got.Should().Contain("a", 1);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldReportUnsuccessfulStatus(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        var response = await client.Api().Get("/missing");

        response.Status.Should().Be(404);
        response.IsSuccessStatusCode.Should().BeFalse();

        var act = () => response.EnsureSuccessStatusCode();
        act.Should()
            .Throw<SurrealDbApiException>()
            .WithMessage("The API GET /missing answered with status 404.");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldDistinguishNullBodyFromNoBody(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        var noBody = await client.Api().Post("/body");
        noBody
            .GetBody<Dictionary<string, bool>>()
            .Should()
            .BeEquivalentTo(new Dictionary<string, bool> { ["none"] = true, ["null"] = false });

        var nullBody = await client.Api().Post("/body", null);
        nullBody
            .GetBody<Dictionary<string, bool>>()
            .Should()
            .BeEquivalentTo(new Dictionary<string, bool> { ["none"] = false, ["null"] = true });

        var nullRequestBody = await client
            .Api()
            .Invoke("/body", new SurrealDbApiRequest { Method = "post", Body = null });
        nullRequestBody.GetBody<Dictionary<string, bool>>()!["null"].Should().BeTrue();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldInvokeWhenSessionHasVariablesOfTheSameName(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        await client.Set("path", "/elsewhere");
        await client.Set("request", "something else");

        var response = await client.Api().Get("/hello");

        response.Status.Should().Be(200);
        response.GetBody<Dictionary<string, string>>().Should().Contain("hello", "world");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldInvokePrefixRoot(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        var response = await client.Api("users/").Get("/");

        response.Path.Should().Be("/users");
        response.Status.Should().Be(200);
        response.GetBody<Dictionary<string, bool>>().Should().Contain("root", true);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldRemoveHeaderWhateverItsCase(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = await CreateClientAsync(
            surrealDbClientGenerator,
            connectionString
        );

        var echo = await client
            .Api("/users")
            .Header("X-Test", "yes")
            .Header("x-test", null)
            .Post("/echo", new Dictionary<string, object?> { ["a"] = 1 });

        echo.GetBody<EchoBody>()!.Header.Should().BeNull();
    }
}
