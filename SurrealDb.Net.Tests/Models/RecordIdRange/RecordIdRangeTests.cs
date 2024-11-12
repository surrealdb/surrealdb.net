namespace SurrealDb.Net.Tests.Models;

public class RecordIdRangeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldFailToCreateRecordIdRangeWithoutTable(string? table)
    {
        var act = () => new RecordIdRange<int, int>(table!, new(), new());
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'table')");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldFailToCreateRecordIdRangeWithoutTableAlt(string? table)
    {
        var act = () => new RecordIdRange<int, int>(table!, new());
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'table')");
    }

    [Fact]
    public void ShouldFailToCreateRecordIdRangeWithoutStartBound()
    {
        var act = () => new RecordIdRange<int, int>("table", new Range<int, int>(default, new()));
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("The start part is not valid (Parameter 'range')");
    }

    [Fact]
    public void ShouldFailToCreateRecordIdRangeWithoutEndBound()
    {
        var act = () => new RecordIdRange<int, int>("table", new Range<int, int>(new(), default));
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("The end part is not valid (Parameter 'range')");
    }

    [Fact]
    public void ShouldCreateRecordIdRange()
    {
        var result = new RecordIdRange<int, int>(
            "table",
            new(-1, RangeBoundType.Inclusive),
            new(1, RangeBoundType.Inclusive)
        );

        result.Table.Should().Be("table");
        result.Range.Start.Should().Be(new RangeBound<int>(-1, RangeBoundType.Inclusive));
        result.Range.End.Should().Be(new RangeBound<int>(1, RangeBoundType.Inclusive));
    }

    [Fact]
    public void ShouldCreateRecordIdRangeAlt()
    {
        var result = new RecordIdRange<int, int>(
            "table",
            new(new(-1, RangeBoundType.Inclusive), new(1, RangeBoundType.Inclusive))
        );

        result.Table.Should().Be("table");
        result.Range.Start.Should().Be(new RangeBound<int>(-1, RangeBoundType.Inclusive));
        result.Range.End.Should().Be(new RangeBound<int>(1, RangeBoundType.Inclusive));
    }
}
