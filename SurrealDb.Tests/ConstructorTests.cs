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

    [Fact]
    public void ShouldUseStaticConstructorForHttpProtocol()
    {
        Func<Uri> func = () => SurrealDbHttpClient.New("localhost:8000").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("http://localhost:8000/");
    }

    [Fact]
    public void ShouldUseStaticConstructorForHttpsProtocol()
    {
        Func<Uri> func = () => SurrealDbHttpsClient.New("cloud.SurrealDb.com").Uri;

        func.Should().NotThrow();
        func().AbsoluteUri.Should().Be("https://cloud.surrealdb.com/");
	}

	[Fact]
	public void ShouldUseStaticConstructorForWsProtocol()
	{
		Func<Uri> func = () => SurrealDbWsClient.New("localhost:8000").Uri;

		func.Should().NotThrow();
		func().AbsoluteUri.Should().Be("ws://localhost:8000/rpc");
	}

	[Fact]
	public void ShouldUseStaticConstructorForWssProtocol()
	{
		Func<Uri> func = () => SurrealDbWssClient.New("cloud.SurrealDb.com").Uri;

		func.Should().NotThrow();
		func().AbsoluteUri.Should().Be("wss://cloud.surrealdb.com/rpc");
	}
}
