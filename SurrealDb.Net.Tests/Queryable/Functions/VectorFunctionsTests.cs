using System.Numerics;

namespace SurrealDb.Net.Tests.Queryable.Functions;

public class VectorFunctionsTests : BaseQueryableTests
{
    [Test]
    public void Add()
    {
        string query = ToSurql(Users.Select(p => Vector2.Add(Vector2.UnitX, Vector2.UnitY)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::add([1, 0], [0, 1]) FROM user
                """
            );
    }

    [Test]
    public void Cross()
    {
        string query = ToSurql(
            Users.Select(p => Vector3.Cross(new Vector3(1, 2, 3), new Vector3(4, 5, 6)))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::cross([1, 2, 3], [4, 5, 6]) FROM user
                """
            );
    }

    [Test]
    public void Distance()
    {
        string query = ToSurql(
            Users.Select(p => Vector3.Distance(new Vector3(10, 50, 200), new Vector3(400, 100, 20)))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::distance::euclidean([10, 50, 200], [400, 100, 20]) FROM user
                """
            );
    }

    [Test]
    public void Divide()
    {
        string query = ToSurql(
            Users.Select(p => Vector3.Divide(new Vector3(1, 2, 3), new Vector3(4, 5, 6)))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::divide([1, 2, 3], [4, 5, 6]) FROM user
                """
            );
    }

    [Test]
    public void Dot()
    {
        string query = ToSurql(
            Users.Select(p => Vector3.Dot(new Vector3(1, 2, 3), new Vector3(1, 2, 3)))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::dot([1, 2, 3], [1, 2, 3]) FROM user
                """
            );
    }

    [Test]
    public void Multiply()
    {
        string query = ToSurql(
            Users.Select(p => Vector3.Multiply(new Vector3(1, 2, 3), new Vector3(1, 2, 3)))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::multiply([1, 2, 3], [1, 2, 3]) FROM user
                """
            );
    }

    [Test]
    public void Normalize()
    {
        string query = ToSurql(Users.Select(p => Vector2.Normalize(new Vector2(4, 3))));

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::normalize([4, 3]) FROM user
                """
            );
    }

    [Test]
    public void MultiplyWithNumber()
    {
        string query = ToSurql(Users.Select(p => Vector3.Multiply(new Vector3(3, 1, 5), 5)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::scale([3, 1, 5], 5) FROM user
                """
            );
    }

    [Test]
    public void MultiplyWithNumberAlt()
    {
        string query = ToSurql(Users.Select(p => Vector3.Multiply(5, new Vector3(3, 1, 5))));

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::scale([3, 1, 5], 5) FROM user
                """
            );
    }

    [Test]
    public void Subtract()
    {
        string query = ToSurql(
            Users.Select(p => Vector3.Subtract(new Vector3(4, 5, 6), new Vector3(1, 2, 3)))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE vector::substract([4, 5, 6], [1, 2, 3]) FROM user
                """
            );
    }
}
