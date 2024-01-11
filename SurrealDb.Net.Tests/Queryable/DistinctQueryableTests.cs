namespace SurrealDb.Net.Tests.Queryable;

public class DistinctQueryableTests : BaseQueryableTests
{
    [Test]
    public void Distinct()
    {
        string query = ToSurql(Users.Select(u => u.Age).Distinct());

        query
            .Should()
            .Be(
                """
                (SELECT array::distinct(Age) AS Values FROM user GROUP ALL)[0].Values
                """
            );
    }
}
