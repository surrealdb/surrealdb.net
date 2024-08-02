using SurrealDb.Net.Internals.Logging;

namespace SurrealDb.Net.Tests.Logging;

public class DbLoggerCategoryTests
{
    [Fact]
    public void ConnectionLoggerCategoryShouldHaveTheCorrectName()
    {
        DbLoggerCategory.Connection.Name.Should().Be("SurrealDB.Connection");
    }

    [Fact]
    public void MethodLoggerCategoryShouldHaveTheCorrectName()
    {
        DbLoggerCategory.Method.Name.Should().Be("SurrealDB.Method");
    }

    [Fact]
    public void QueryLoggerCategoryShouldHaveTheCorrectName()
    {
        DbLoggerCategory.Query.Name.Should().Be("SurrealDB.Query");
    }
}
