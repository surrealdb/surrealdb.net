using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Tests;

public class SurrealDbOptionsBuilderTests
{
	[Fact]
	public void ShouldCreateEmpty()
	{
		var options = new SurrealDbOptionsBuilder()
			.Build();

		options.Endpoint.Should().BeNull();
		options.Namespace.Should().BeNull();
		options.Database.Should().BeNull();
		options.Username.Should().BeNull();
		options.Password.Should().BeNull();
	}

	[Fact]
	public void ShouldCreateWithEndpoint()
	{
		var options = new SurrealDbOptionsBuilder()
			.WithEndpoint("http://localhost:8000")
			.Build();

		options.Endpoint.Should().Be("http://localhost:8000");
		options.Namespace.Should().BeNull();
		options.Database.Should().BeNull();
		options.Username.Should().BeNull();
		options.Password.Should().BeNull();
	}

	[Fact]
	public void ShouldCreateWithNamespace()
	{
		var options = new SurrealDbOptionsBuilder()
			.WithNamespace("namespace")
			.Build();

		options.Endpoint.Should().BeNull();
		options.Namespace.Should().Be("namespace");
		options.Database.Should().BeNull();
		options.Username.Should().BeNull();
		options.Password.Should().BeNull();
	}

	[Fact]
	public void ShouldCreateWithDatabase()
	{
		var options = new SurrealDbOptionsBuilder()
			.WithDatabase("database")
			.Build();

		options.Endpoint.Should().BeNull();
		options.Namespace.Should().BeNull();
		options.Database.Should().Be("database");
		options.Username.Should().BeNull();
		options.Password.Should().BeNull();
	}

	[Fact]
	public void ShouldCreateWithUsername()
	{
		var options = new SurrealDbOptionsBuilder()
			.WithUsername("username")
			.Build();

		options.Endpoint.Should().BeNull();
		options.Namespace.Should().BeNull();
		options.Database.Should().BeNull();
		options.Username.Should().Be("username");
		options.Password.Should().BeNull();
	}

	[Fact]
	public void ShouldCreateWithPassword()
	{
		var options = new SurrealDbOptionsBuilder()
			.WithPassword("password")
			.Build();

		options.Endpoint.Should().BeNull();
		options.Namespace.Should().BeNull();
		options.Database.Should().BeNull();
		options.Username.Should().BeNull();
		options.Password.Should().Be("password");
	}

	[Fact]
	public void ShouldCreateFromConnectionString()
	{
		string connectionString = "Server=http://localhost:8000;Namespace=test;Database=test;Username=root;Password=root";

		var options = new SurrealDbOptionsBuilder()
			.FromConnectionString(connectionString)
			.Build();

		options.Endpoint.Should().Be("http://localhost:8000");
		options.Namespace.Should().Be("test");
		options.Database.Should().Be("test");
		options.Username.Should().Be("root");
		options.Password.Should().Be("root");
	}

	[Fact]
	public void ShouldCreateFromAlternativeConnectionString()
	{
		string connectionString = "Endpoint=http://localhost:8000;NS=test;DB=test;User=root;Pass=root";

		var options = new SurrealDbOptionsBuilder()
			.FromConnectionString(connectionString)
			.Build();

		options.Endpoint.Should().Be("http://localhost:8000");
		options.Namespace.Should().Be("test");
		options.Database.Should().Be("test");
		options.Username.Should().Be("root");
		options.Password.Should().Be("root");
	}
}
