namespace SurrealDb.Net.Tests;

public class ConstructorTests
{
    [Test]
    public void ShouldSupportHttpProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("http://127.0.0.1:8000").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("http://127.0.0.1:8000/");
    }

    [Test]
    public void ShouldSupportHttpsProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("https://cloud.SurrealDb.com").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("https://cloud.surrealdb.com/");
    }

    [Test]
    public void ShouldSupportWsProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("ws://127.0.0.1:8000/rpc").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("ws://127.0.0.1:8000/rpc");
    }

    [Test]
    public void ShouldSupportWssProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("wss://cloud.SurrealDb.com/rpc").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("wss://cloud.surrealdb.com/rpc");
    }

    [Test]
    [Arguments("ws://127.0.0.1:8000", "ws://127.0.0.1:8000/rpc")]
    [Arguments("wss://cloud.SurrealDb.com", "wss://cloud.surrealdb.com/rpc")]
    [Arguments("ws://127.0.0.1:8000/", "ws://127.0.0.1:8000/rpc")]
    [Arguments("wss://cloud.SurrealDb.com/", "wss://cloud.surrealdb.com/rpc")]
    [Arguments("ws://127.0.0.1:8000/bar", "ws://127.0.0.1:8000/bar/rpc")]
    [Arguments("wss://cloud.SurrealDb.com/bar", "wss://cloud.surrealdb.com/bar/rpc")]
    public void ShouldAutomaticallyAddRpcSuffixForWsProtocols(string endpoint, string expected)
    {
        Func<Uri> func = () => new SurrealDbClient(endpoint).Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be(expected);
    }

    [Test]
    public void ShouldRequireDependencyInjectionForMemoryProtocol()
    {
        Action action = () => new SurrealDbClient("mem://");

        action
            .Should()
            .Throw<Exception>()
            .WithMessage(
                "Impossible to create a new in-memory SurrealDB client. Make sure to use `AddInMemoryProvider`."
            );
    }

    [Test]
    public void ShouldThrowErrorOnInvalidUri()
    {
        Action act = () => new SurrealDbClient("123456");

        act.Should().Throw<UriFormatException>();
    }

    [Test]
    public void ShouldThrowErrorOnUnsupportedProtocol()
    {
        Action act = () => new SurrealDbClient("abc://cloud.SurrealDb.com");

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("The protocol 'abc' is not supported.");
    }
}
