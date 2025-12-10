namespace SurrealDb.Net.Tests.Queryable;

public class ParametersQueryableTests : BaseQueryableTests
{
    private readonly VerifySettings _verifySettings = new();

    public ParametersQueryableTests()
    {
        _verifySettings.UseDirectory("Snapshots");
    }

    [Test]
    public void ConstantParameter()
    {
        const int minAge = 18;
        string query = ToSurql(Users.Where(u => u.Age >= minAge));

        query
            .Should()
            .Be(
                """
                SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age >= 18
                """
            );
        Parameters.Should().BeEmpty();
    }

    [Test]
    public Task SingleParameter()
    {
        int minAge = 18;
        string query = ToSurql(Users.Where(u => u.Age >= minAge));

        query
            .Should()
            .Be(
                """
                SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age >= $minAge
                """
            );
        return Verify(Parameters, _verifySettings);
    }

    [Test]
    public Task MultipleParameters()
    {
        string country = "USA";
        string city = "New York";
        string query = ToSurql(
            Addresses.Where(a => a.IsActive && a.Country == country && a.City == city)
        );

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id AS Id, IsActive, Number, State, Street, ZipCode FROM address WHERE IsActive && Country == $country && City == $city
                """
            );
        return Verify(Parameters, _verifySettings);
    }

    private class AdressSearch
    {
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    [Test]
    public Task ObjectParameter()
    {
        var search = new AdressSearch { Country = "USA", City = "New York" };
        string query = ToSurql(
            Addresses.Where(a => a.IsActive && a.Country == search.Country && a.City == search.City)
        );

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id AS Id, IsActive, Number, State, Street, ZipCode FROM address WHERE IsActive && Country == $search.Country && City == $search.City
                """
            );
        return Verify(Parameters, _verifySettings);
    }

    [Test]
    public Task NullParameter()
    {
        string? status = null;
        string query = ToSurql(Posts.Where(p => p.Status == status));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post WHERE status == $status
                """
            );
        return Verify(Parameters, _verifySettings);
    }

    [Test]
    public Task ReservedParameterNames()
    {
        // List of reserved parameter names:
        // https://github.com/surrealdb/surrealdb/blob/d5134c71e543cc7f9997237d11ad907f02857b76/crates/core/src/cnf/mod.rs#L15
        string access = "USA";
        string auth = "USA";
        string token = "USA";
        string session = "USA";

        string query = ToSurql(
            Addresses.Where(a =>
                a.Country == access
                || a.Country == auth
                || a.Country == token
                || a.Country == session
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id AS Id, IsActive, Number, State, Street, ZipCode FROM address WHERE Country == $_access || Country == $_auth || Country == $_token || Country == $_session
                """
            );
        return Verify(Parameters, _verifySettings);
    }
}
