namespace SurrealDb.Net.Tests.Queryable.Functions;

public class EnumerableFunctionsTests : BaseQueryableTests
{
    [Test]
    public void Any()
    {
        string query = ToSurql(Users.Where(p => new[] { 1, 10, 20 }.Any()).Select(p => p.Username));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE !array::is_empty([1, 10, 20])
                """
            );
    }

    [Test]
    public void Append()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Append(30)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::append([1, 10, 20], 30) FROM user
                """
            );
    }

    [Test]
    public void Chunk()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 30 }.Chunk(3)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::clump([1, 10, 20, 30], 3) FROM user
                """
            );
    }

    [Test]
    public void Concat()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Concat(new[] { p.Age })));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::concat([1, 10, 20], [Age]) FROM user
                """
            );
    }

    [Test]
    public void Contains()
    {
        string query = ToSurql(
            Users.Where(p => new[] { 1, 10, 20 }.Contains(p.Age)).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE [1, 10, 20] CONTAINS Age
                """
            );
    }

    [Test]
    public void Count()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.Count()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::len([1, 10, 20, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void Distinct()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.Distinct()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::distinct([1, 10, 20, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void ElementAtOrDefault()
    {
        string query = ToSurql(
            Users.Select(p => new[] { 1, 10, 20, 10, 20 }.ElementAtOrDefault(2))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::at([1, 10, 20, 10, 20], 2) FROM user
                """
            );
    }

    [Test]
    public void Empty()
    {
        string query = ToSurql(Users.Select(p => Enumerable.Empty<int>()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE [] FROM user
                """
            );
    }

    [Test]
    public void Except()
    {
        string query = ToSurql(Orders.Select(o => o.Products.Except(o.Products)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::difference(Products, Products) FROM order
                """
            );
    }

    [Test]
    public void FirstOrDefault()
    {
        string query = ToSurql(Orders.Select(o => o.Products.FirstOrDefault()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::first(Products) FROM order
                """
            );
    }

    [Test]
    public void Intersect()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Intersect(new[] { 1, 2, 3 })));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::intersect([1, 10, 20], [1, 2, 3]) FROM user
                """
            );
    }

    [Test]
    public void LastOrDefault()
    {
        string query = ToSurql(Orders.Select(o => o.Products.LastOrDefault()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::last(Products) FROM order
                """
            );
    }

    [Test]
    public void Max()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.Max()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::max([1, 10, 20, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void Min()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.Min()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::min([1, 10, 20, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void Order()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.Order()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::sort::asc([1, 10, 20, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void OrderDescending()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.OrderDescending()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::sort::desc([1, 10, 20, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void Prepend()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Prepend(0)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::prepend([1, 10, 20], 0) FROM user
                """
            );
    }

    [Test]
    public void Range()
    {
        string query = ToSurql(
            Users.Where(p => Enumerable.Range(5, 10).Contains(p.Age)).Select(p => p.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE 5..(5 + 10) CONTAINS Age
                """
            );
    }

    [Test]
    public void Repeat()
    {
        string query = ToSurql(Users.Select(p => Enumerable.Repeat("hello", 2)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::repeat("hello", 2) FROM user
                """
            );
    }

    [Test]
    public void Reverse()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Reverse()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::reverse([1, 10, 20]) FROM user
                """
            );
    }

    [Test]
    public void Skip()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Skip(2)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::slice([1, 10, 20], 2) FROM user
                """
            );
    }

    [Test]
    public void Take()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 2, 3, 4, 5 }.Take(1)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::slice([1, 2, 3, 4, 5], 0, 1) FROM user
                """
            );
    }

    [Test]
    public void ToHashSet()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20, 10, 20 }.ToHashSet()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE <set> [1, 10, 20, 10, 20] FROM user
                """
            );
    }

    [Test]
    public void Union()
    {
        string query = ToSurql(Users.Select(p => new[] { 1, 10, 20 }.Union(new[] { 1, 2, 3 })));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::union([1, 10, 20], [1, 2, 3]) FROM user
                """
            );
    }
}
