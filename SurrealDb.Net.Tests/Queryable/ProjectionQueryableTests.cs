namespace SurrealDb.Net.Tests.Queryable;

public class ProjectionQueryableTests : BaseQueryableTests
{
    [Test]
    public void MapAllFieldsFromEntity()
    {
        string query = ToSurql(Users);

        query
            .Should()
            .Be(
                """
                SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user
                """
            );
    }

    [Test]
    public void MapAllFieldsFromEntityWithColumnAttribute()
    {
        string query = ToSurql(Posts);

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post
                """
            );
    }

    [Test]
    public void MapSingleField()
    {
        string query = ToSurql(Users.Select(u => u.Username));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user
                """
            );
    }

    [Test]
    public void MapSingleFieldWithColumnAttribute()
    {
        string query = ToSurql(Posts.Select(p => p.CreatedAt));

        query
            .Should()
            .Be(
                """
                SELECT VALUE created_at FROM post
                """
            );
    }

    [Test]
    public void MapSingleFieldToArray()
    {
        string query = ToSurql(Posts.Select(p => new[] { 1, 2, 3 }));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [1, 2, 3] FROM post
                """
            );
    }

    [Test]
    public void MapFieldsUsingAnonymousType()
    {
        string query = ToSurql(Users.Select(u => new { u.Username, u.Age }));

        query
            .Should()
            .Be(
                """
                SELECT Age, Username FROM user
                """
            );
    }

    [Test]
    public void MapFieldsUsingAnonymousTypeAlt()
    {
        string query = ToSurql(
            Users.Select(u => new
            {
                u.Username,
                u.IsActive,
                HasAllRights = u.IsAdmin && u.IsOwner,
            })
        );

        query
            .Should()
            .Be(
                """
                SELECT IsAdmin && IsOwner AS HasAllRights, IsActive, Username FROM user
                """
            );
    }

    public class MappedUser
    {
        public string Username { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Test]
    public void MapFieldsToObject()
    {
        string query = ToSurql(
            Users.Select(u => new MappedUser { Username = u.Username, Age = u.Age })
        );

        query
            .Should()
            .Be(
                """
                SELECT Age, Username FROM user
                """
            );
    }

    public class MappedUserAlt
    {
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool HasAllRights { get; set; }
    }

    [Test]
    public void MapFieldsToObjectAlt()
    {
        string query = ToSurql(
            Users.Select(u => new MappedUserAlt
            {
                Username = u.Username,
                IsActive = u.IsActive,
                HasAllRights = u.IsAdmin && u.IsOwner,
            })
        );

        query
            .Should()
            .Be(
                """
                SELECT IsAdmin && IsOwner AS HasAllRights, IsActive, Username FROM user
                """
            );
    }

    [Test]
    public void SelectMany()
    {
        string query = ToSurql(Orders.SelectMany(o => o.Products));

        query
            .Should()
            .Be(
                """
                (SELECT array::flatten(Products) AS Values FROM order GROUP ALL)[0].Values
                """
            );
    }
}
