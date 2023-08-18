namespace SurrealDb.Tests.Models;

public class ConstructorsDurationTests
{
	[Fact]
	public void NewDurationShouldBeEqualToZeroDuration()
	{
		bool result = new Duration() == Duration.Zero;
		result.Should().BeTrue();
	}
}
