namespace SurrealDb.Net.Tests.Queryable.Value;

public class BinaryValueTests : BaseQueryableTests
{
    [Test]
    public void Add()
    {
        string query = ToSurql(Users.Where(p => p.Age + 5 > 18));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age + 5 > 18
                """
            );
    }

    [Test]
    public void AddWithString()
    {
        string query = ToSurql(Addresses.Where(a => (a.State + " " + a.ZipCode) == ""));

        query
            .Should()
            .Be(
                """"
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE State + " " + ZipCode == ""
                """"
            );
    }

    [Test]
    public void Subtract()
    {
        string query = ToSurql(Users.Where(p => p.Age - 5 > 18));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age - 5 > 18
                """
            );
    }

    [Test]
    [Skip("Implement Cast + TimeSpan value")]
    public void SubtractWithTimeSpan()
    {
        string query = ToSurql(
            Posts.Where(p => p.CreatedAt - TimeSpan.FromDays(30) > DateTime.Now)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE (CreatedAt - 4w2d) FROM post
                """
            );
    }

    [Test]
    public void Multiply()
    {
        string query = ToSurql(Users.Where(u => u.Age * 2 > 18));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age * 2 > 18
                """
            );
    }

    [Test]
    public void Divide()
    {
        string query = ToSurql(Users.Where(u => u.Age / 2 > 18));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age / 2 > 18
                """
            );
    }

    [Test]
    public void AndAlso()
    {
        string query = ToSurql(Users.Where(u => u.IsAdmin && u.IsActive));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE IsAdmin && IsActive
                """
            );
    }

    [Test]
    public void OrElse()
    {
        string query = ToSurql(Users.Where(u => u.IsAdmin || u.IsOwner));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE IsAdmin || IsOwner
                """
            );
    }

    [Test]
    public void Equal()
    {
        string query = ToSurql(Posts.Where(p => p.Title == "Title 1"));

        query
            .Should()
            .Be(
                """
                SELECT content, created_at, id, status, title FROM post WHERE title == "Title 1"
                """
            );
    }

    [Test]
    public void NotEqual()
    {
        string query = ToSurql(Posts.Where(p => p.Title != "Title 1"));

        query
            .Should()
            .Be(
                """
                SELECT content, created_at, id, status, title FROM post WHERE title != "Title 1"
                """
            );
    }

    [Test]
    public void LessThan()
    {
        string query = ToSurql(Addresses.Where(a => a.Number < 15));

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE Number < 15
                """
            );
    }

    [Test]
    public void LessThanOrEqual()
    {
        string query = ToSurql(Addresses.Where(a => a.Number <= 15));

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE Number <= 15
                """
            );
    }

    [Test]
    public void GreaterThan()
    {
        string query = ToSurql(Addresses.Where(a => a.Number > 15));

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE Number > 15
                """
            );
    }

    [Test]
    public void GreaterThanOrEqual()
    {
        string query = ToSurql(Addresses.Where(a => a.Number >= 15));

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id, IsActive, Number, State, Street, ZipCode FROM address WHERE Number >= 15
                """
            );
    }

    [Test]
    public void Modulo()
    {
        string query = ToSurql(Users.Where(u => u.Age % 2 == 0));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE Age % 2 == 0
                """
            );
    }

    [Test]
    public void Coalesce()
    {
        string query = ToSurql(Posts.Where(p => (p.Status ?? "DRAFT") == "DRAFT"));

        query
            .Should()
            .Be(
                """
                SELECT content, created_at, id, status, title FROM post WHERE status ?? "DRAFT" == "DRAFT"
                """
            );
    }

    [Test]
    public void And()
    {
        Func<string> func = () => ToSurql(Users.Where(p => p.IsActive & p.IsAdmin));

        func.Should().Throw<NotSupportedException>("Bitwise operations are not supported.");
    }

    [Test]
    public void Or()
    {
        Func<string> func = () => ToSurql(Users.Where(p => p.IsActive | p.IsAdmin));

        func.Should().Throw<NotSupportedException>("Bitwise operations are not supported.");
    }

    [Test]
    public void ExclusiveOr()
    {
        Func<string> func = () => ToSurql(Users.Where(p => p.IsActive ^ p.IsAdmin));

        func.Should().Throw<NotSupportedException>("Bitwise operations are not supported.");
    }

    [Test]
    public void LeftShift()
    {
        int a = 0b001;
        int b = 0b100;
        Func<string> func = () => ToSurql(Users.Where(p => a << b > 0));

        func.Should().Throw<NotSupportedException>("Bitwise operations are not supported.");
    }

    [Test]
    public void RightShift()
    {
        int a = 0b001;
        int b = 0b000;
        Func<string> func = () => ToSurql(Users.Where(p => a >> b > 0));

        func.Should().Throw<NotSupportedException>("Bitwise operations are not supported.");
    }
}
