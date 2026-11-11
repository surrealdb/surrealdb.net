namespace SurrealDb.Net.Tests.Queryable.Members;

public class EnumerableMembersTests : BaseQueryableTests
{
    [Test]
    public void Length()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Length));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::len([1, 10, 20]) FROM user
                """
            );
    }
}
