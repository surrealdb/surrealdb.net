namespace SurrealDb.Net.Tests.Queryable.Members;

public class DateTimeMembersTests : BaseQueryableTests
{
    [Test]
    public void Now()
    {
        string query = ToSurql(Orders.Where(o => o.CreatedAt < DateTime.Now).Select(o => o.Status));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Status FROM order WHERE CreatedAt < time::now()
                """
            );
    }

    [Test]
    public void UtcNow()
    {
        string query = ToSurql(
            Orders.Where(o => o.CreatedAt < DateTime.UtcNow).Select(o => o.Status)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Status FROM order WHERE CreatedAt < time::now()
                """
            );
    }

    [Test]
    public void UnixEpoch()
    {
        string query = ToSurql(
            Orders.Where(o => o.CreatedAt < DateTime.UnixEpoch).Select(o => o.Status)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Status FROM order WHERE CreatedAt < time::from::unix(0)
                """
            );
    }

    [Test]
    public void Today()
    {
        string query = ToSurql(
            Orders.Where(o => o.CreatedAt < DateTime.Today).Select(o => o.Status)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Status FROM order WHERE CreatedAt < time::floor(time::now(), 1d)
                """
            );
    }

    [Test]
    public void Year()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.Year));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().year() FROM order
                """
            );
    }

    [Test]
    public void Month()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.Month));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().month() FROM order
                """
            );
    }

    [Test]
    public void Day()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.Day));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().day() FROM order
                """
            );
    }

    [Test]
    public void Hour()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.Hour));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().hour() FROM order
                """
            );
    }

    [Test]
    public void Minute()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.Minute));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().minute() FROM order
                """
            );
    }

    [Test]
    public void Second()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.Second));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().second() FROM order
                """
            );
    }

    [Test]
    public void DayOfYear()
    {
        string query = ToSurql(Orders.Select(o => DateTime.Now.DayOfYear));

        query
            .Should()
            .Be(
                """
                SELECT VALUE time::now().yday() FROM order
                """
            );
    }
}
