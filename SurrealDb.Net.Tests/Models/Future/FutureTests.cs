namespace SurrealDb.Net.Tests.Models.Future;

public class FutureTests
{
    [Fact]
    public void ShouldApplyToString()
    {
        var future = new SurrealDb.Net.Models.Future("fn::myFunction()");
        future.ToString().Should().Be("<future> { fn::myFunction() }");
    }
}
