using SurrealDb.Net.Internals.Logging;

namespace SurrealDb.Net.Tests.Logging;

public class DbLoggerCategoryTests
{
    [Test]
    public void ConnectionLoggerCategoryShouldHaveTheCorrectName()
    {
        DbLoggerCategory.Connection.Name.Should().Be("SurrealDB.Connection");
    }

    [Test]
    public void MethodLoggerCategoryShouldHaveTheCorrectName()
    {
        DbLoggerCategory.Method.Name.Should().Be("SurrealDB.Method");
    }

    [Test]
    public void QueryLoggerCategoryShouldHaveTheCorrectName()
    {
        DbLoggerCategory.Query.Name.Should().Be("SurrealDB.Query");
    }
}
