using Range = SurrealDb.Net.Models.Range;

namespace SurrealDb.Net.Tests.Models;

public class RangeTests
{
    [Fact]
    public void ShouldCreateFullRange()
    {
        var result = new Range<int, int>();

        result.Start.Should().BeNull();
        result.End.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateStandardRange()
    {
        var result = new Range<int, string>(
            new(0, RangeBoundType.Inclusive),
            new("x", RangeBoundType.Exclusive)
        );

        result.Start.Should().Be(new RangeBound<int>(0, RangeBoundType.Inclusive));
        result.End.Should().Be(new RangeBound<string>("x", RangeBoundType.Exclusive));
    }

    [Fact]
    public void ShouldCreateStartOnlyRange()
    {
        var result = new Range<int, string>(new(0, RangeBoundType.Inclusive), default);

        result.Start.Should().Be(new RangeBound<int>(0, RangeBoundType.Inclusive));
        result.End.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateEndOnlyRange()
    {
        var result = new Range<int, string>(default, new("x", RangeBoundType.Exclusive));

        result.Start.Should().BeNull();
        result.End.Should().Be(new RangeBound<string>("x", RangeBoundType.Exclusive));
    }

    [Fact]
    public void ShouldCreateFullRangeFromStaticClass()
    {
        var result = Range.Full();

        result.Start.Should().BeNull();
        result.End.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateStartOnlyRangeFromStaticClass()
    {
        var result = Range.StartFrom<int>(new(0, RangeBoundType.Inclusive));

        result.Start.Should().Be(new RangeBound<int>(0, RangeBoundType.Inclusive));
        result.End.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateEndOnlyRangeFromStaticClass()
    {
        var result = Range.EndTo<string>(new("x", RangeBoundType.Exclusive));

        result.Start.Should().BeNull();
        result.End.Should().Be(new RangeBound<string>("x", RangeBoundType.Exclusive));
    }
}
