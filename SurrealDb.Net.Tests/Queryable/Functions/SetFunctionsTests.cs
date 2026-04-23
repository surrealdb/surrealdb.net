namespace SurrealDb.Net.Tests.Queryable.Functions;

public class SetFunctionsTests : BaseQueryableTests
{
    [Test]
    [Skip("Add is mutable")]
    public void Add()
    {
        string query = ToSurql(Users.Select(p => p.Tags.Add("new_tag")));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::add(Tags, "new_tag") FROM user
                """
            );
    }

    [Test]
    public void Any()
    {
        string query = ToSurql(
            Users.Where(p => new HashSet<int> { 1, 10, 20 }.Any()).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE !set::is_empty({1, 10, 20})
                """
            );
    }

    [Test]
    public void Contains()
    {
        string query = ToSurql(
            Users.Where(p => new HashSet<int> { 1, 10, 20 }.Contains(p.Age)).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE {1, 10, 20} CONTAINS Age
                """
            );
    }

    [Test]
    public void Count()
    {
        string query = ToSurql(Users.Select(p => new HashSet<int> { 1, 10, 20, 10, 20 }.Count()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::len({1, 10, 20, 10, 20}) FROM user
                """
            );
    }

    [Test]
    public void Except()
    {
        string query = ToSurql(
            Users.Select(p =>
                new HashSet<int> { 1, 2, 3, 4 }.Except(new HashSet<int> { 3, 4, 5, 6 })
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::difference({1, 2, 3, 4}, {3, 4, 5, 6}) FROM user
                """
            );
    }

    [Test]
    public void FirstOrDefault()
    {
        string query = ToSurql(Users.Select(p => p.Tags.FirstOrDefault()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::first(Tags) FROM user
                """
            );
    }

    [Test]
    public void Intersect()
    {
        string query = ToSurql(
            Users.Select(p =>
                new HashSet<int> { 1, 2, 3, 4 }.Intersect(new HashSet<int> { 3, 4, 5, 6 })
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::intersect({1, 2, 3, 4}, {3, 4, 5, 6}) FROM user
                """
            );
    }

    [Test]
    public void LastOrDefault()
    {
        string query = ToSurql(Users.Select(p => p.Tags.LastOrDefault()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::last(Tags) FROM user
                """
            );
    }

    [Test]
    public void Max()
    {
        string query = ToSurql(Users.Select(p => new HashSet<int> { 1, 10, 20 }.Max()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::max({1, 10, 20}) FROM user
                """
            );
    }

    [Test]
    public void Min()
    {
        string query = ToSurql(Users.Select(p => new HashSet<int> { 1, 10, 20 }.Min()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::min({1, 10, 20}) FROM user
                """
            );
    }

    [Test]
    [Skip("Remove is mutable")]
    public void Remove()
    {
        string query = ToSurql(Users.Select(p => p.Tags.Remove("old_tag")));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::remove(Tags, "old_tag") FROM user
                """
            );
    }

    [Test]
    public void Skip()
    {
        string query = ToSurql(Users.Select(p => new HashSet<int> { 1, 2, 3, 4, 5 }.Skip(2)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::slice({1, 2, 3, 4, 5}, 2) FROM user
                """
            );
    }

    [Test]
    public void Take()
    {
        string query = ToSurql(Users.Select(p => new HashSet<int> { 1, 2, 3, 4, 5 }.Take(3)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::slice({1, 2, 3, 4, 5}, 0, 3) FROM user
                """
            );
    }

    [Test]
    public void ToArray()
    {
        string query = ToSurql(Users.Select(p => new HashSet<int> { 1, 10, 20 }.ToArray()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE <array> {1, 10, 20} FROM user
                """
            );
    }

    [Test]
    public void Union()
    {
        string query = ToSurql(
            Users.Select(p => new HashSet<int> { 1, 2, 3 }.Union(new HashSet<int> { 2, 3, 4 }))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE set::union({1, 2, 3}, {2, 3, 4}) FROM user
                """
            );
    }
}
