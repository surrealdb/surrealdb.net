namespace SurrealDb.Net.Tests.Queryable.Members;

public class TimeSpanMembersTests : BaseQueryableTests
{
    [Test]
    public void Zero()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.Zero));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 0ns FROM user
                """
            );
    }
}
