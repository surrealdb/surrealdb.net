namespace SurrealDb.Net.Tests.Models;

public class RangeBoundTests
{
    [Test]
    public void ShouldCreateEmptyRangeBound()
    {
        var bound = new RangeBound<int>();

        bound.Value.Should().Be(0);
        bound.Type.Should().Be(RangeBoundType.Unknown);
    }

    [Test]
    public void ShouldCreateInclusiveRangeBound()
    {
        var bound = new RangeBound<int>(14, RangeBoundType.Inclusive);

        bound.Value.Should().Be(14);
        bound.Type.Should().Be(RangeBoundType.Inclusive);
    }

    [Test]
    public void ShouldCreateExclusiveRangeBound()
    {
        var bound = new RangeBound<string>("v", RangeBoundType.Exclusive);

        bound.Value.Should().Be("v");
        bound.Type.Should().Be(RangeBoundType.Exclusive);
    }

    [Test]
    public void ShouldCreateInclusiveRangeBoundFromStaticClass()
    {
        var bound = RangeBound.Inclusive("incl");

        bound.Value.Should().Be("incl");
        bound.Type.Should().Be(RangeBoundType.Inclusive);
    }

    [Test]
    public void ShouldCreateExclusiveRangeBoundFromStaticClass()
    {
        var bound = RangeBound.Exclusive("excl");

        bound.Value.Should().Be("excl");
        bound.Type.Should().Be(RangeBoundType.Exclusive);
    }
}
