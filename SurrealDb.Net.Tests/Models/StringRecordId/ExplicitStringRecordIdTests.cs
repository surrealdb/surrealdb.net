namespace SurrealDb.Net.Tests.Models;

public class ExplicitStringRecordIdTests
{
    [Test]
    public void ShouldCreateStringRecordIdFromStringExplicitly()
    {
        var recordId = (StringRecordId)"table:id";

        recordId.Value.Should().Be("table:id");
    }
}
