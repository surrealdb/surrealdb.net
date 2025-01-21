namespace SurrealDb.Net.Tests.Models;

public class ConstructorsDurationTests
{
    [Test]
    public void ConstructDurationShouldBeEqualToZeroDuration()
    {
        bool result = new Duration() == Duration.Zero;
        result.Should().BeTrue();
    }

    [Test]
    public void ConstructDurationShouldFailWithUnauthorizedFloatingPointValue()
    {
        var action = () => new Duration("17.3ms");
        action.Should().Throw<Exception>().WithMessage("Invalid unit");
    }

    [Test]
    public void ConstructDurationWithFloatingPointValue()
    {
        var result = new Duration("17.3ms", true);
        result.MilliSeconds.Should().Be(17);
    }
}
