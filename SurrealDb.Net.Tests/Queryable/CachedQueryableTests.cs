namespace SurrealDb.Net.Tests.Queryable;

public class CachedQueryableTests : BaseQueryableTests
{
    private IQueryable<int> CachedSimple => Users.Select(u => u.Age).Cached();

    [Test]
    public void Simple()
    {
        string query = ToSurql(CachedSimple);

        query
            .Should()
            .Be(
                """
                SELECT VALUE Age FROM user
                """
            );
        Parameters.Should().BeEmpty();

        string queryCached = ToSurql(CachedSimple);
        query.Should().BeEquivalentTo(queryCached);
        Parameters.Should().BeEmpty();
    }

    private IQueryable<Models.Address> CachedWithParams(string country, string city) =>
        Addresses.Where(a => a.IsActive && a.Country == country && a.City == city).Cached();

    [Test]
    public void WithParams()
    {
        string query = ToSurql(CachedWithParams("USA", "New York"));

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE IsActive && Country == $country && City == $city
                """
            );
        Parameters.Should().Contain("country", "USA");
        Parameters.Should().Contain("city", "New York");

        string queryCached = ToSurql(CachedWithParams("USA", "Los Angeles"));
        query.Should().BeEquivalentTo(queryCached);
        Parameters.Should().Contain("country", "USA");
        Parameters.Should().Contain("city", "Los Angeles");
    }

    [Test]
    public void NotCachedTwice()
    {
        string country = "USA";
        string city = "New York";

        Func<string> queryFn = () =>
            ToSurql(
                Addresses
                    .Cached()
                    .Where(a => a.IsActive && a.Country == country && a.City == city)
                    .Cached()
            );

        queryFn
            .Should()
            .ThrowExactly<InvalidOperationException>()
            .WithMessage("A query cannot be cached multiple times.");
    }

    [Test]
    public void Successive()
    {
        string query1 = ToSurql(CachedWithParams("USA", "New York"));

        query1
            .Should()
            .Be(
                """
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE IsActive && Country == $country && City == $city
                """
            );
        Parameters.Should().Contain("country", "USA");
        Parameters.Should().Contain("city", "New York");

        string query2 = ToSurql(CachedSimple);

        query2
            .Should()
            .Be(
                """
                SELECT VALUE Age FROM user
                """
            );
        Parameters.Should().BeEmpty();

        string query3 = ToSurql(CachedWithParams("USA", "Los Angeles"));

        query3.Should().BeEquivalentTo(query1);
        Parameters.Should().Contain("country", "USA");
        Parameters.Should().Contain("city", "Los Angeles");
    }
}
