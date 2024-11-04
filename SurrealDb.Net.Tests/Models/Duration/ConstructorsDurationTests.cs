namespace SurrealDb.Net.Tests.Models;

public class ConstructorsDurationTests
{
    [Fact]
    public void ConstructDurationShouldBeEqualToZeroDuration()
    {
        bool result = new Duration() == Duration.Zero;
        result.Should().BeTrue();
    }

    [Fact]
    public void ConstructDurationShouldFailWithUnauthorizedFloatingPointValue()
    {
        var action = () => new Duration("17.3ms");
        action.Should().Throw<Exception>().WithMessage("Invalid unit");
    }

    [Fact]
    public void ConstructDurationWithFloatingPointValue()
    {
        var result = new Duration("17.3ms", true);
        result.MilliSeconds.Should().Be(17);
    }
}
