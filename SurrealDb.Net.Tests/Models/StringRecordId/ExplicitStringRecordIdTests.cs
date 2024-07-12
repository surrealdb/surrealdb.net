namespace SurrealDb.Net.Tests.Models;

public class ExplicitStringRecordIdTests
{
    [Fact]
    public void ShouldCreateStringRecordIdFromStringExplicitly()
    {
        var recordId = (StringRecordId)"table:id";

        recordId.Value.Should().Be("table:id");
    }
}
