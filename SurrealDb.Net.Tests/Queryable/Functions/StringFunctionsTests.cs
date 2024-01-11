namespace SurrealDb.Net.Tests.Queryable.Functions;

public class StringFunctionsTests : BaseQueryableTests
{
    [Test]
    public void Contains()
    {
        string query = ToSurql(Users.Where(p => p.Username.Contains("__")).Select(p => p.Username));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE string::contains(Username, "__")
                """
            );
    }

    [Test]
    public void Concat()
    {
        string query = ToSurql(Addresses.Select(a => string.Concat(a.City, " ", a.Country)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::concat(City, " ", Country) FROM address
                """
            );
    }

    [Test]
    public void ConcatArray()
    {
        string query = ToSurql(
            Addresses.Select(a => string.Concat(new[] { a.City, " ", a.Country }))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::concat(City, " ", Country) FROM address
                """
            );
    }

    [Test]
    public void EndsWith()
    {
        string query = ToSurql(Users.Where(p => p.Username.EndsWith('0')).Select(p => p.Username));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE string::ends_with(Username, "0")
                """
            );
    }

    [Test]
    public void IsNullOrEmpty()
    {
        string query = ToSurql(
            Users.Where(p => !string.IsNullOrEmpty(p.Username)).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE !!Username
                """
            );
    }

    [Test]
    public void IsNullOrWhiteSpace()
    {
        string query = ToSurql(
            Users.Where(p => !string.IsNullOrWhiteSpace(p.Username)).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE !(!Username || !string::words(Username))
                """
            );
    }

    [Test]
    public void Join()
    {
        string query = ToSurql(
            Addresses.Select(p => string.Join(' ', new[] { p.City, p.Country }))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::join([City, Country], " ") FROM address
                """
            );
    }

    [Test]
    public void Replace()
    {
        string query = ToSurql(Users.Select(p => p.Username.Replace(" ", "_")));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::replace(Username, " ", "_") FROM user
                """
            );
    }

    [Test]
    public void SplitDefault()
    {
        string query = ToSurql(Posts.Select(p => p.Title.Split(" ", StringSplitOptions.None)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::split(title, " ") FROM post
                """
            );
    }

    [Test]
    public void SplitRemoveEmptyEntries()
    {
        Func<string> fn = () =>
            ToSurql(Posts.Select(p => p.Title.Split(" ", StringSplitOptions.RemoveEmptyEntries)));

        fn.Should()
            .Throw<InvalidCastException>()
            .WithMessage("The second argument of string.Split must be StringSplitOptions.None.");
    }

    [Test]
    public void SplitTrimEntries()
    {
        Func<string> fn = () =>
            ToSurql(Posts.Select(p => p.Title.Split(" ", StringSplitOptions.TrimEntries)));

        fn.Should()
            .Throw<InvalidCastException>()
            .WithMessage("The second argument of string.Split must be StringSplitOptions.None.");
    }

    [Test]
    public void StartsWith()
    {
        string query = ToSurql(
            Users.Where(p => p.Username.StartsWith('0')).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE string::starts_with(Username, "0")
                """
            );
    }

    [Test]
    public void Substring()
    {
        string query = ToSurql(Users.Select(p => p.Username.Substring(5)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::slice(5) FROM user
                """
            );
    }

    [Test]
    public void SubstringWithLength()
    {
        string query = ToSurql(Users.Select(p => p.Username.Substring(5, 2)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::slice(5, 2) FROM user
                """
            );
    }

    [Test]
    public void Trim()
    {
        string query = ToSurql(Users.Select(p => p.Username.Trim()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::trim(Username) FROM user
                """
            );
    }

    [Test]
    public void ToLower()
    {
        string query = ToSurql(Users.Select(p => p.Username.ToLower()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::lowercase(Username) FROM user
                """
            );
    }

    [Test]
    public void ToUpper()
    {
        string query = ToSurql(Users.Select(p => p.Username.ToUpper()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::uppercase(Username) FROM user
                """
            );
    }
}
