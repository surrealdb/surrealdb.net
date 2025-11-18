namespace SurrealDb.Net.Tests.Queryable.Value;

public class UnaryValueTests : BaseQueryableTests
{
    [Test]
    public void Negate()
    {
        bool filter = false;
        string query = ToSurql(Posts.Where(p => !filter));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post WHERE !$filter
                """
            );
    }

    [Test]
    public void Positive()
    {
        int value = 5;
        string query = ToSurql(Posts.Where(p => +value > 0));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post WHERE $value > 0
                """
            );
    }

    [Test]
    public void Negative()
    {
        int value = 5;
        string query = ToSurql(Posts.Where(p => -value > 0));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post WHERE -$value > 0
                """
            );
    }

    [Test]
    public void ArrayLength()
    {
        string query = ToSurql(Orders.Where(o => o.Products.Length > 0));

        query
            .Should()
            .Be(
                """
                SELECT Address, CreatedAt, id AS Id, Products, Status FROM order WHERE array::len(Products) > 0
                """
            );
    }
}
