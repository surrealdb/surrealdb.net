namespace SurrealDb.Net.Tests.Models;

public class ConstructorsStringRecordIdTests
{
    [Fact]
    public void ThrowExceptionIfNoValueProvided()
    {
        var act = () => new StringRecordId();

        act.Should().Throw<InvalidOperationException>().WithMessage("A value is required");
    }

    [Fact]
    public void ShouldCreateStringRecordId()
    {
        var recordId = new StringRecordId("table:id");

        recordId.Value.Should().Be("table:id");
    }
}
