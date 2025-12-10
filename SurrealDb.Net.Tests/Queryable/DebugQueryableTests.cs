namespace SurrealDb.Net.Tests.Queryable;

/// <summary>
/// Sandbox mode used to test any query
/// </summary>
public class DebugQueryableTests : BaseQueryableTests
{
    [Test]
    [Skip("Sandbox")]
    public void Debug()
    {
        //string query = ToSurql(Users.Where(u => u.Age > Math.PI));
        string query = ToSurql(Posts.Where(p => p.CreatedAt > DateTime.Now));

        query
            .Should()
            .Be(
                """
                SELECT * FROM post WHERE Title == "Title 1"
                """
            );
    }
}
