namespace SurrealDb.Net.Tests.Queryable;

public class NestedQueryableTests : BaseQueryableTests
{
    [Test]
    public void SimpleNestedQuery()
    {
        string query = ToSurql(
            Products
                .Where(p => Orders.SelectMany(o => o.Products).Contains(p.Id))
                .Select(p => p.Name)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Name FROM product WHERE (SELECT array::flatten(Products) AS Values FROM order GROUP ALL)[0].Values CONTAINS id
                """
            );
    }

    [Test]
    public void ParentQuery()
    {
        string query = ToSurql(
            Orders.SelectMany(o =>
                Addresses
                    .Where(a => a.Id! == o.Address.Id!)
                    .Select(a => new
                    {
                        o.Id,
                        a.City,
                        a.Country,
                    })
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT array::flatten((SELECT City, Country, $parent.id AS Id FROM address WHERE id == $parent.Address.id)) AS Values FROM order GROUP ALL)[0].Values
                """
            );
    }

    [Test]
    public void GrandParentQuery()
    {
        string query = ToSurql(
            Orders.SelectMany(o1 =>
                Orders.SelectMany(o2 =>
                    Addresses
                        .Where(a => a.Id! == o1.Address.Id!)
                        .Select(a => new
                        {
                            OriginalId = o1.Id,
                            o2.Id,
                            a.City,
                            a.Country,
                        })
                )
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT array::flatten((SELECT array::flatten((SELECT City, Country, $parent.id AS Id, $parent.$parent.id AS OriginalId FROM address WHERE id == $parent.$parent.Address.id)) AS Values FROM order GROUP ALL)[0].Values) AS Values FROM order GROUP ALL)[0].Values
                """
            );
    }
}
