namespace SurrealDb.Tests;

public class ThingTests
{
    [Fact]
    public void ShouldCreateThingFromTwoArguments()
    {
        var thing = new Thing("table", "id");

        thing.Table.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("id");
        thing.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldCreateThingFromStringId()
    {
        var thing = new Thing("table:id");

        thing.Table.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("id");
        thing.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldFailToCreateThingIfNoSeparator()
    {
        Action act = () => new Thing("just_id");

        act.Should().Throw<ArgumentException>().WithMessage("Cannot detect separator on Thing (Parameter 'thing')");
    }
}
