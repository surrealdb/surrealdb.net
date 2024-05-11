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
        options.Serialization.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateWithEndpoint()
    {
        var options = new SurrealDbOptionsBuilder().WithEndpoint("http://127.0.0.1:8000").Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
        options.Serialization.Should().BeNull();
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
        options.Serialization.Should().BeNull();
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
        options.Serialization.Should().BeNull();
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
        options.Serialization.Should().BeNull();
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
        options.Serialization.Should().BeNull();
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
        options.Serialization.Should().BeNull();
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
        options.Serialization.Should().BeNull();
    }

    [Fact]
    public void ShouldFailToCreateWithIncorrectNamingPolicy()
    {
        Action act = () => new SurrealDbOptionsBuilder().WithNamingPolicy("test").Build();

        act.Should().Throw<ArgumentException>().WithParameterName("namingPolicy");
    }

    [Theory]
    [InlineData("JSON")]
    [InlineData("CBOR")]
    public void ShouldCreateWithSerialization(string serialization)
    {
        var options = new SurrealDbOptionsBuilder().WithSerialization(serialization).Build();

        options.Endpoint.Should().BeNull();
        options.Namespace.Should().BeNull();
        options.Database.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
        options.Serialization.Should().Be(serialization);
    }

    [Fact]
    public void ShouldFailToCreateWithIncorrectSerialization()
    {
        Action act = () => new SurrealDbOptionsBuilder().WithSerialization("test").Build();

        act.Should().Throw<ArgumentException>().WithParameterName("serialization");
    }

    [Fact]
    public void ShouldCreateFromConnectionString()
    {
        var connectionString =
            "Server=http://127.0.0.1:8000;Namespace=test;Database=test;Username=root;Password=root;NamingPolicy=CamelCase;Serialization=CBOR";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().Be("root");
        options.Password.Should().Be("root");
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().Be("CamelCase");
        options.Serialization.Should().Be("CBOR");
    }

    [Fact]
    public void ShouldCreateFromAlternativeConnectionString()
    {
        var connectionString = "Endpoint=http://127.0.0.1:8000;NS=test;DB=test;User=root;Pass=root";

        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();

        options.Endpoint.Should().Be("http://127.0.0.1:8000");
        options.Namespace.Should().Be("test");
        options.Database.Should().Be("test");
        options.Username.Should().Be("root");
        options.Password.Should().Be("root");
        options.Token.Should().BeNull();
        options.NamingPolicy.Should().BeNull();
        options.Serialization.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateFromConnectionStringWithAccessToken()
    {
        var connectionString =
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
        options.Serialization.Should().BeNull();
    }
}
