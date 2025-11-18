namespace SurrealDb.Net.Tests.Queryable;

public class FilteringQueryableTests : BaseQueryableTests
{
    [Test]
    public void ShouldFilterWithStringConstantEquality()
    {
        string query = ToSurql(Posts.Where(p => p.Title == "Title 1"));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post WHERE title == "Title 1"
                """
            );
    }

    [Test]
    public void ShouldFilterWithMultipleBooleanLogic()
    {
        string query = ToSurql(Users.Where(u => (u.IsAdmin || u.IsOwner) && u.IsActive));

        query
            .Should()
            .Be(
                """
                SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE (IsAdmin || IsOwner) && IsActive
                """
            );
    }

    [Test]
    public void ShouldFilterWithMultiplePredicates()
    {
        string query = ToSurql(
            Posts.Where(p => p.Title == "Title 1").Where(p => p.Status != "DRAFT")
        );

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post WHERE title == "Title 1" && status != "DRAFT"
                """
            );
    }
}
