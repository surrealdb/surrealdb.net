namespace SurrealDb.Net.Tests.Queryable.Functions;

public class DateTimeFunctionsTests : BaseQueryableTests
{
    [Test]
    public void Add()
    {
        string query = ToSurql(Users.Select(p => DateTime.Now.Add(TimeSpan.Zero)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now() + 0ns FROM user
                """
            );
    }

    [Test]
    public void Subtract()
    {
        string query = ToSurql(Users.Select(p => DateTime.Now.Subtract(TimeSpan.Zero)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now() - 0ns FROM user
                """
            );
    }
}
