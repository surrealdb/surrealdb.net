namespace SurrealDb.Net.Tests.Queryable;

public class OrderingQueryableTests : BaseQueryableTests
{
    [Test]
    public void ShouldOrderByAsc()
    {
        string query = ToSurql(Posts.OrderBy(p => p.CreatedAt));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post ORDER BY created_at
                """
            );
    }

    [Test]
    public void ShouldOrderByDesc()
    {
        string query = ToSurql(Posts.OrderByDescending(p => p.CreatedAt));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post ORDER BY created_at DESC
                """
            );
    }

    [Test]
    public void ShouldOrderByMultipleFieldsAsc()
    {
        string query = ToSurql(
            Posts.OrderBy(p => p.CreatedAt).ThenBy(p => p.Status).ThenBy(p => p.Title)
        );

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post ORDER BY created_at, status, title
                """
            );
    }

    [Test]
    public void ShouldOrderByMultipleFieldsEitherAscOrDesc()
    {
        string query = ToSurql(
            Posts
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Status)
                .ThenBy(p => p.Title)
        );

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post ORDER BY created_at DESC, status DESC, title
                """
            );
    }

    [Test]
    public void ShouldSkipPreviousOrderExpressions()
    {
        string query = ToSurql(
            Posts
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Title)
                .OrderBy(p => p.Status)
                .OrderByDescending(p => p.Status)
        );

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post ORDER BY status DESC
                """
            );
    }
}
