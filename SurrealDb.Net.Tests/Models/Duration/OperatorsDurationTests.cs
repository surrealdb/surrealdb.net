namespace SurrealDb.Net.Tests.Models;

public class OperatorsDurationTests
{
    [Test]
    public void TwoDurationsWithSameValueShouldBeEqual()
    {
        var duration1 = new Duration("1h");
        var duration2 = new Duration("1h");

        bool result = duration1 == duration2;

        result.Should().BeTrue();
    }

    [Test]
    public void TwoDurationsWithDifferentDurationUnitsShouldNotBeEqual()
    {
        var duration1 = new Duration("1m");
        var duration2 = new Duration("1h");

        bool result = duration1 == duration2;

        result.Should().BeFalse();
    }

    [Test]
    public void TwoDurationsWithDifferentValueShouldNotBeEqual()
    {
        var duration1 = new Duration("2h");
        var duration2 = new Duration("1h");

        bool result = duration1 == duration2;

        result.Should().BeFalse();
    }

    [Test]
    public void TwoDurationsWithSameValueShouldBeEqualEvenWithExtra0()
    {
        var duration1 = new Duration("0d1h");
        var duration2 = new Duration("1h0s");

        bool result = duration1 == duration2;

        result.Should().BeTrue();
    }

    [Test]
    public void ShouldBeLowerThan()
    {
        var duration1 = new Duration("5m");
        var duration2 = new Duration("1h");

        bool result = duration1 < duration2;

        result.Should().BeTrue();
    }

    [Test]
    public void ShouldBeLowerThanOrEqualTo()
    {
        var duration1 = new Duration("1h");
        var duration2 = new Duration("1h");

        bool result = duration1 <= duration2;

        result.Should().BeTrue();
    }

    [Test]
    public void ShouldBeGreaterThan()
    {
        var duration1 = new Duration("5y2m");
        var duration2 = new Duration("1h");

        bool result = duration1 > duration2;

        result.Should().BeTrue();
    }

    [Test]
    public void ShouldBeGreaterThanOrEqualTo()
    {
        var duration1 = new Duration("1h");
        var duration2 = new Duration("1h");

        bool result = duration1 >= duration2;

        result.Should().BeTrue();
    }
}
