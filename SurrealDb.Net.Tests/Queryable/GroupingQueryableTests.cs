namespace SurrealDb.Net.Tests.Queryable;

public class GroupingQueryableTests : BaseQueryableTests
{
    [Test]
    public void DefaultGroupingBySingleField()
    {
        string query = ToSurql(Posts.GroupBy(p => p.Status));

        const string fieldsProjection =
            "content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title";
        const string fullProjection = $"SELECT {fieldsProjection} FROM post";

        query
            .Should()
            .Be(
                $"""
                SELECT status AS Key, ({fullProjection} WHERE status == $parent.status) AS Values FROM (SELECT status FROM post GROUP BY status)
                """
            );
    }

    [Test]
    public void ShouldGroupBySingleField()
    {
        string query = ToSurql(Posts.GroupBy(p => p.Status).Select(g => g.Key));

        query
            .Should()
            .Be(
                """
                SELECT VALUE status FROM (SELECT status FROM post GROUP BY status)
                """
            );
    }

    [Test]
    public void ShouldGroupByMultipleFields()
    {
        string query = ToSurql(
            Addresses.GroupBy(a => new { a.Country, a.City }).Select(g => g.Key)
        );

        // TODO : target
        // SELECT Country, City FROM address GROUP BY Country, City
        query
            .Should()
            .Be(
                """
                SELECT City, Country FROM (SELECT City, Country FROM address GROUP BY City, Country)
                """
            );
    }

    [Test]
    public void ShouldGroupBySingleFieldAndAggregateCountViaProjection()
    {
        string query = ToSurql(Posts.GroupBy(p => p.Status).Select(g => g.Count()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Value FROM (SELECT status, count() AS Value FROM post GROUP BY status)
                """
            );
    }

    [Test]
    public void ShouldGroupBySingleFieldAndAggregateSumViaProjection()
    {
        string query = ToSurql(Users.GroupBy(p => p.IsActive).Select(g => g.Sum(v => v.Age)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Value FROM (SELECT IsActive, math::sum(Age) AS Value FROM user GROUP BY IsActive)
                """
            );
    }

    [Test]
    public void ShouldGroupBySingleFieldAndAggregateMinViaProjection()
    {
        string query = ToSurql(Users.GroupBy(p => p.IsActive).Select(g => g.Min(v => v.Age)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Value FROM (SELECT IsActive, array::min(Age) AS Value FROM user GROUP BY IsActive)
                """
            );
    }

    [Test]
    public void ShouldGroupBySingleFieldAndAggregateMaxViaProjection()
    {
        string query = ToSurql(Users.GroupBy(p => p.IsActive).Select(g => g.Max(v => v.Age)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Value FROM (SELECT IsActive, array::max(Age) AS Value FROM user GROUP BY IsActive)
                """
            );
    }

    [Test]
    public void ShouldGroupBySingleFieldAndAggregateAvgViaProjection()
    {
        string query = ToSurql(Users.GroupBy(p => p.IsActive).Select(g => g.Average(v => v.Age)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE Value FROM (SELECT IsActive, math::mean(Age) AS Value FROM user GROUP BY IsActive)
                """
            );
    }

    // TODO : Project to anonymous type

    // [Test]
    // [Skip("TODO")]
    // public void ShouldGroupBySingleFieldAndAggregateSumViaProjection()
    // {
    //     string query = ToSurql(Posts.GroupBy(p => p.Status).Select(g => g..Sum()));
    //
    //     query
    //         .Should()
    //         .Be(
    //             """
    //             SELECT count() FROM post GROUP BY Status
    //             """
    //         );
    // }
}
