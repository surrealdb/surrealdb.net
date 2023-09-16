using SurrealDB.NET.Tests.Fixtures;

using Xunit.Abstractions;

namespace SurrealDB.NET.Tests;

[Trait("Category", "HTTP")]
[Collection(SurrealDbCollectionFixture.Name)]
public sealed class SurrealHttpTests : IDisposable
{
	private readonly SurrealDbFixture _fixture;
	private readonly ServiceFixture _di;

	public SurrealHttpTests(SurrealDbFixture fixture, ITestOutputHelper xunit)
	{
		_fixture = fixture;
		_di = new ServiceFixture( xunit, _fixture.Port);
	}

	public void Dispose()
	{
		_di.Dispose();
	}

	[Fact(DisplayName = "/health on healthy service", Timeout = 5000)]
	public async Task Health()
	{
		var health = await _di.Http.HealthAsync();

		Assert.True(health);
	}
}
