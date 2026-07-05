using SurrealDb.Net.Tests.Queryable.Models;

namespace SurrealDb.Net.Tests.Queryable;

public class GraphQueryableTests : BaseQueryableTests
{
    [Test]
    public void ShouldNavigateOutThroughRelation()
    {
        string query = ToSurql(Users.Select(u => u.Out<Purchased, StoreProduct>()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->purchased->product FROM user
                """
            );
    }

    [Test]
    public void ShouldNavigateInThroughRelation()
    {
        string query = ToSurql(Products.Select(p => p.In<Purchased, StoreUser>()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this<-purchased<-user FROM product
                """
            );
    }

    [Test]
    public void ShouldChainGraphTraversals()
    {
        string query = ToSurql(
            Products.Select(p =>
                p.In<Purchased, StoreUser>().SelectMany(u => u.Out<Purchased, StoreProduct>())
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::flatten($this<-purchased<-user->purchased->product) FROM product
                """
            );
    }

    [Test]
    public void ShouldProjectChainedGraphTraversalMember()
    {
        string query = ToSurql(
            Products.Select(p =>
                p.In<Purchased, StoreUser>()
                    .SelectMany(u => u.Out<Purchased, StoreProduct>().Select(p => p.Name))
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::flatten($this<-purchased<-user->purchased->product.name) FROM product
                """
            );
    }
}
