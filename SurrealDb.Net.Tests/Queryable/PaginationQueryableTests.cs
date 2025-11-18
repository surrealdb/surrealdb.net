namespace SurrealDb.Net.Tests.Queryable;

public class PaginationQueryableTests : BaseQueryableTests
{
    [Test]
    public void ShouldSkipFromTable()
    {
        string query = ToSurql(Posts.Skip(10));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post START 10
                """
            );
    }

    [Test]
    public void ShouldLimitFromTable()
    {
        string query = ToSurql(Posts.Take(5));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post LIMIT 5
                """
            );
    }

    [Test]
    public void ShouldLimitAndSkipFromTable()
    {
        string query = ToSurql(Posts.Skip(10).Take(5));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post LIMIT 5 START 10
                """
            );
    }
}
