namespace SurrealDb.Net.Tests.Queryable.Functions;

public class ArrayFunctionsTests : BaseQueryableTests
{
    [Test]
    public void IndexOf()
    {
        string query = ToSurql(
            Users.Select(p => Array.IndexOf(new[] { "cat", "badger", "dog", "octopus" }, "octopus"))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::find_index(["cat", "badger", "dog", "octopus"], "octopus") FROM user
                """
            );
    }
}
