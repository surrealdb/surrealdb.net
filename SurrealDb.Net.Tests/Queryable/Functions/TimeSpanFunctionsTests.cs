namespace SurrealDb.Net.Tests.Queryable.Functions;

public class TimeSpanFunctionsTests : BaseQueryableTests
{
    [Test]
    public void FromDays()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromDays(1)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1d FROM user
                """
            );
    }

    [Test]
    public void FromHours()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromHours(4)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 4h FROM user
                """
            );
    }

    [Test]
    public void FromMinutes()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromMinutes(2)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 2m FROM user
                """
            );
    }

    [Test]
    public void FromSeconds()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromSeconds(18)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 18s FROM user
                """
            );
    }

    [Test]
    public void FromMilliseconds()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromMilliseconds(144, 0)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 144ms FROM user
                """
            );
    }

    [Test]
    public void FromMicroseconds()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromMicroseconds(265)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 265us FROM user
                """
            );
    }

    [Test]
    public void FromTicks()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromTicks(123456789)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE duration::from::nanos(100 * 123456789) FROM user
                """
            );
    }

    [Test]
    public void Add()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromHours(1).Add(TimeSpan.Zero)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1h + 0ns FROM user
                """
            );
    }

    [Test]
    public void Subtract()
    {
        string query = ToSurql(Users.Select(p => TimeSpan.FromHours(1).Subtract(TimeSpan.Zero)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1h - 0ns FROM user
                """
            );
    }
}
