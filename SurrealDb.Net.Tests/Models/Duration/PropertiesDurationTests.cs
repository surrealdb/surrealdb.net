namespace SurrealDb.Net.Tests.Models;

public class PropertiesDurationTests
{
    [Test]
    public void ZeroDurationShouldHaveZeroValue()
    {
        var duration = Duration.Zero;

        duration.NanoSeconds.Should().Be(0);
        duration.MilliSeconds.Should().Be(0);
        duration.MicroSeconds.Should().Be(0);
        duration.Seconds.Should().Be(0);
        duration.Minutes.Should().Be(0);
        duration.Hours.Should().Be(0);
        duration.Days.Should().Be(0);
        duration.Weeks.Should().Be(0);
        duration.Years.Should().Be(0);
    }

    [Test]
    public void ZeroDurationShouldBe0ns()
    {
        var duration = Duration.Zero;

        duration.ToString().Should().Be("0ns");
    }

    [Test]
    public void ZeroDurationShouldBeZeroTimeSpan()
    {
        var duration = Duration.Zero;

        duration.ToTimeSpan().Should().Be(new());
    }
}
