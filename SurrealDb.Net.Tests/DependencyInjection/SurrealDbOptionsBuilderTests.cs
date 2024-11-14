using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Net.Tests.DependencyInjection;

public class SurrealDbOptionsBuilderTests
{
    [Fact]
    public void ShouldCreateEmpty()
    {
        var options = new SurrealDbOptionsBuilder().Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Theory]
    [InlineData("mem://")]
    [InlineData("rocksdb://db.data")]
    [InlineData("surrealkv://db.data")]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("https://cloud.surrealdb.com")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    [InlineData("wss://cloud.surrealdb.com/rpc")]
    public void ShouldCreateWithEndpoint(string endpoint)
    {
        var options = new SurrealDbOptionsBuilder().WithEndpoint(endpoint).Build();

        options.Endpoint.Should().Be(endpoint);
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateWithNamespace()
    {
        var options = new SurrealDbOptionsBuilder().WithNamespace("namespace").Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().Be("namespace");
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateWithDatabase()
    {
        var options = new SurrealDbOptionsBuilder().WithDatabase("database").Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().Be("database");
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateWithUsername()
    {
        var options = new SurrealDbOptionsBuilder().WithUsername("username").Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().Be("username");
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateWithPassword()
    {
        var options = new SurrealDbOptionsBuilder().WithPassword("password").Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().Be("password");
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateWithToken()
    {
        var options = new SurrealDbOptionsBuilder()
            .WithToken(
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
            )
            .Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options
            .Token.Should()
            .Be(
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
            );
        options.NamingPolicy.Should().BeNull();
    }

    [Theory]
    [InlineData("CamelCase")]
    [InlineData("SnakeCaseLower")]
    [InlineData("SnakeCaseUpper")]
    [InlineData("KebabCaseLower")]
    [InlineData("KebabCaseUpper")]
    public void ShouldCreateWithNamingPolicy(string namingPolicy)
    {
        var options = new SurrealDbOptionsBuilder().WithNamingPolicy(namingPolicy).Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().Be(namingPolicy);
    }

    [Fact]
    public void ShouldFailToCreateWithIncorrectNamingPolicy()
    {
        Action act = () => new SurrealDbOptionsBuilder().WithNamingPolicy("test").Build();

        act.Should().Throw<ArgumentException>().WithParameterName("namingPolicy");
    }

    [Fact]
    public void ShouldCreateFromConnectionString()
    {
        string connectionString =
            "Server=http://127.0.0.1:8000;Namespace=test;Database=test;Username=root;Password=root;NamingPolicy=CamelCase";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().Be("root");
        options.Password.Should().Be("root");
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().Be("CamelCase");
    }

    [Fact]
    public void ShouldCreateFromAlternativeConnectionString()
    {
        string connectionString =
            "Endpoint=http://127.0.0.1:8000;NS=test;DB=test;User=root;Pass=root";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().Be("root");
        options.Password.Should().Be("root");
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateFromConnectionStringWithLeadingSemiColon()
    {
        string connectionString =
            "Endpoint=http://127.0.0.1:8000;NS=test;DB=test;User=root;Pass=root;";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().Be("root");
        options.Password.Should().Be("root");
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateFromConnectionStringWithAccessToken()
    {
        string connectionString =
            "Endpoint=http://127.0.0.1:8000;NS=test;DB=test;Token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options
            .Token.Should()
            .Be(
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
            );
        options.NamingPolicy.Should().BeNull();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("https://cloud.surrealdb.com")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    [InlineData("wss://cloud.surrealdb.com/rpc")]
    public void ShouldCreateFromConnectionStringWithServerEndpoint(string server)
    {
        string connectionString = $"Server={server}";
        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be(server);
    }

    [Theory]
    [InlineData("mem://")]
    [InlineData("rocksdb://db.data")]
    [InlineData("surrealkv://db.data")]
    public void ShouldFailToCreateFromConnectionStringWithServerEndpoint(string server)
    {
        string connectionString = $"Server={server}";
        Action action = () =>
            new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        action
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("connectionString")
            .WithMessage($"Invalid server endpoint: {server} (Parameter 'connectionString')");
    }

    [Theory]
    [InlineData("mem://")]
    [InlineData("rocksdb://db.data")]
    [InlineData("surrealkv://db.data")]
    public void ShouldCreateFromConnectionStringWithClientEndpoint(string client)
    {
        string connectionString = $"Client={client}";
        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be(client);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("https://cloud.surrealdb.com")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    [InlineData("wss://cloud.surrealdb.com/rpc")]
    public void ShouldFailToCreateFromConnectionStringWithClientEndpoint(string client)
    {
        string connectionString = $"Client={client}";
        Action action = () =>
            new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        action
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("connectionString")
            .WithMessage($"Invalid client endpoint: {client} (Parameter 'connectionString')");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ShouldSetSensitiveDataLoggingEnabled(bool value, bool expected)
    {
        var options = new SurrealDbOptionsBuilder().EnableSensitiveDataLogging(value).Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
        options.Logging.Should().NotBeNull();
        options.Logging.SensitiveDataLoggingEnabled.Should().Be(expected);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("with=equals", "with=equals")]
    [InlineData("&!ayhiof-]}k(w'2.&)o,qh4", "&!ayhiof-]}k(w'2.&)o,qh4")]
    [InlineData("'surrounded'", "surrounded")]
    [InlineData("\"surrounded'2\"", "surrounded'2")]
    [InlineData("{surrounded'3}", "surrounded'3")]
    [InlineData("'with;semi-colon'", "with;semi-colon")]
    [InlineData("\"cn$r;'d^u_s4dm%^zr!frxqf\"", "cn$r;'d^u_s4dm%^zr!frxqf")]
    [InlineData("{cn$r;'d^u_s4dm%^zr!frxqf}", "cn$r;'d^u_s4dm%^zr!frxqf")]
    public void ShouldParseConnectionStringWithSpecialCharsInPassword(
        string passwordInput,
        string? expectedPassword
    )
    {
        string connectionString =
            $"Endpoint=http://127.0.0.1:8000;NS=test;DB=test;User=root;Password={passwordInput}";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().Be("root");
        options.Password.Should().Be(expectedPassword);
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
    }

    [Theory]
    [InlineData("with;semi-colon")]
    [InlineData("cn$r;'d^u_s4dm%^zr!frxqf")]
    [InlineData("'cn$r;'d^u_s4dm%^zr!frxqf'")]
    public void ShouldFailToParseConnectionStringWithSpecialCharsInPassword(string passwordInput)
    {
        string connectionString =
            $"Endpoint=http://127.0.0.1:8000;NS=test;DB=test;User=root;Password={passwordInput}";

        var act = () =>
            new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                $"Invalid connection string: {connectionString} (Parameter 'connectionString')"
            );
    }
}
