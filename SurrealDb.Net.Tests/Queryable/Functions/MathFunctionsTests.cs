namespace SurrealDb.Net.Tests.Queryable.Functions;

public class MathFunctionsTests : BaseQueryableTests
{
    [Test]
    public void Abs()
    {
        string query = ToSurql(Users.Select(p => Math.Abs(-10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::abs(-10) FROM user
                """
            );
    }

    [Test]
    public void Acos()
    {
        string query = ToSurql(Users.Select(p => Math.Acos(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::acos(10) FROM user
                """
            );
    }

    [Test]
    public void Asin()
    {
        string query = ToSurql(Users.Select(p => Math.Asin(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::asin(10) FROM user
                """
            );
    }

    [Test]
    public void Atan()
    {
        string query = ToSurql(Users.Select(p => Math.Atan(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::atan(10) FROM user
                """
            );
    }

    [Test]
    public void Ceiling()
    {
        string query = ToSurql(Users.Select(p => Math.Ceiling(10d)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::ceil(10) FROM user
                """
            );
    }

    [Test]
    public void Clamp()
    {
        string query = ToSurql(Users.Select(p => Math.Clamp(4, 1, 10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::clamp(4, 1, 10) FROM user
                """
            );
    }

    [Test]
    public void Cos()
    {
        string query = ToSurql(Users.Select(p => Math.Cos(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::cos(10) FROM user
                """
            );
    }

    [Test]
    public void Floor()
    {
        string query = ToSurql(Users.Select(p => Math.Floor(10d)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::floor(10) FROM user
                """
            );
    }

    [Test]
    public void Log()
    {
        string query = ToSurql(Users.Select(p => Math.Log(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::log(10) FROM user
                """
            );
    }

    [Test]
    public void Log2()
    {
        string query = ToSurql(Users.Select(p => Math.Log2(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::log2(10) FROM user
                """
            );
    }

    [Test]
    public void Log10()
    {
        string query = ToSurql(Users.Select(p => Math.Log10(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::log10(10) FROM user
                """
            );
    }

    [Test]
    public void Max()
    {
        string query = ToSurql(Users.Select(p => Math.Max(5, 12)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::max(5, 12) FROM user
                """
            );
    }

    [Test]
    public void Min()
    {
        string query = ToSurql(Users.Select(p => Math.Min(5, 12)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::min(5, 12) FROM user
                """
            );
    }

    [Test]
    public void Pow()
    {
        string query = ToSurql(Users.Select(p => Math.Pow(2, 5)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::pow(2, 5) FROM user
                """
            );
    }

    [Test]
    public void Round()
    {
        string query = ToSurql(Users.Select(p => Math.Round(10d)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::round(10) FROM user
                """
            );
    }

    [Test]
    public void Sign()
    {
        string query = ToSurql(Users.Select(p => Math.Sign(10d)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::sign(10) FROM user
                """
            );
    }

    [Test]
    public void Sin()
    {
        string query = ToSurql(Users.Select(p => Math.Sin(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::sin(10) FROM user
                """
            );
    }

    [Test]
    public void Sqrt()
    {
        string query = ToSurql(Users.Select(p => Math.Sqrt(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::sqrt(10) FROM user
                """
            );
    }

    [Test]
    public void Tan()
    {
        string query = ToSurql(Users.Select(p => Math.Tan(10)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::tan(10) FROM user
                """
            );
    }
}
