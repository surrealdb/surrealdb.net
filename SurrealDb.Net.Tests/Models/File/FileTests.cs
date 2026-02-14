namespace SurrealDb.Net.Tests.Models.File;

public class FileTests
{
    [Test]
    public void ShouldApplyToString()
    {
        var file = new SurrealFile("bucket", "some/key/to/a/file.txt");
        file.ToString().Should().Be("bucket:/some/key/to/a/file.txt");
    }
}
