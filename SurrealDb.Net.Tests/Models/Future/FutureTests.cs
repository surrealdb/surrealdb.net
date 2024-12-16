namespace SurrealDb.Net.Tests.Models;

public class FutureTests
{
    [Fact]
    public void ShouldApplyToString()
    {
        var future = new Future("fn::myFunction()");
        future.ToString().Should().Be("<future> { fn::myFunction() }");
    }
}
