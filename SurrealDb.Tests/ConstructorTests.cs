namespace SurrealDb.Tests;

public class ConstructorTests
{
    [Fact]
    public void ShouldSupportHttpProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("http://localhost:8000").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("http://localhost:8000/");
    }

    [Fact]
    public void ShouldSupportHttpsProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("https://cloud.SurrealDb.com").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("https://cloud.surrealdb.com/");
    }

	[Fact]
	public void ShouldSupportWsProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("ws://localhost:8000/rpc").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("ws://localhost:8000/rpc");
    }

    [Fact]
    public void ShouldSupportWssProtocol()
    {
        Func<Uri> func = () => new SurrealDbClient("wss://cloud.SurrealDb.com/rpc").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("wss://cloud.surrealdb.com/rpc");
    }

    [Fact]
    public void ShouldThrowErrorOnInvalidUri()
    {
        Action act = () => new SurrealDbClient("123456");

        act.Should().Throw<UriFormatException>();
    }

    [Fact]
    public void ShouldThrowErrorOnUnsupportedProtocol()
    {
        Action act = () => new SurrealDbClient("abc://cloud.SurrealDb.com");

        act.Should().Throw<ArgumentException>().WithMessage("This protocol is not supported.");
    }

    [Theory]
	[InlineData("localhost:8000", "http://localhost:8000/")]
	[InlineData("localhost:80", "http://localhost/")]
	[InlineData("localhost", "http://localhost/")]
	public void ShouldUseStaticConstructorForHttpProtocol(string host, string expected)
    {
        Func<Uri> func = () => SurrealDbHttpClient.New(host).Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be(expected);
    }

	[Theory]
	[InlineData("cloud.SurrealDb.com:8000", "https://cloud.surrealdb.com:8000/")]
	[InlineData("cloud.SurrealDb.com:443", "https://cloud.surrealdb.com/")]
	[InlineData("cloud.SurrealDb.com", "https://cloud.surrealdb.com/")]
	public void ShouldUseStaticConstructorForHttpsProtocol(string host, string expected)
    {
        Func<Uri> func = () => SurrealDbHttpsClient.New(host).Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be(expected);
	}

	[Theory]
	[InlineData("localhost:8000", "ws://localhost:8000/rpc")]
	[InlineData("localhost:80", "ws://localhost/rpc")]
	[InlineData("localhost", "ws://localhost/rpc")]
	public void ShouldUseStaticConstructorForWsProtocol(string host, string expected)
	{
		Func<Uri> func = () => SurrealDbWsClient.New(host).Uri;

		func.Should().NotThrow();
		func().AbsoluteUri.Should().Be(expected);
	}

	[Theory]
	[InlineData("cloud.SurrealDb.com:8000", "wss://cloud.surrealdb.com:8000/rpc")]
	[InlineData("cloud.SurrealDb.com:443", "wss://cloud.surrealdb.com/rpc")]
	[InlineData("cloud.SurrealDb.com", "wss://cloud.surrealdb.com/rpc")]
	public void ShouldUseStaticConstructorForWssProtocol(string host, string expected)
	{
		Func<Uri> func = () => SurrealDbWssClient.New(host).Uri;

		func.Should().NotThrow();
		func().AbsoluteUri.Should().Be(expected);
	}
}
