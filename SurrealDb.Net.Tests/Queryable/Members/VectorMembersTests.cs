using System.Numerics;

namespace SurrealDb.Net.Tests.Queryable.Members;

public class VectorMembersTests : BaseQueryableTests
{
    [Test]
    public void Zero()
    {
        string query = ToSurql(Users.Select(p => Vector4.Zero));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [0, 0, 0, 0] FROM user
                """
            );
    }

    [Test]
    public void One()
    {
        string query = ToSurql(Users.Select(p => Vector4.One));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [1, 1, 1, 1] FROM user
                """
            );
    }

    [Test]
    public void UnitX()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitX));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [1, 0, 0, 0] FROM user
                """
            );
    }

    [Test]
    public void UnitY()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitY));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [0, 1, 0, 0] FROM user
                """
            );
    }

    [Test]
    public void UnitZ()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitZ));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [0, 0, 1, 0] FROM user
                """
            );
    }

    [Test]
    public void UnitW()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitW));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [0, 0, 0, 1] FROM user
                """
            );
    }

    [Test]
    public void X()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitX.X));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1 FROM user
                """
            );
    }

    [Test]
    public void Y()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitY.Y));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1 FROM user
                """
            );
    }

    [Test]
    public void Z()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitZ.Z));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1 FROM user
                """
            );
    }

    [Test]
    public void W()
    {
        string query = ToSurql(Users.Select(p => Vector4.UnitW.W));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 1 FROM user
                """
            );
    }
}
