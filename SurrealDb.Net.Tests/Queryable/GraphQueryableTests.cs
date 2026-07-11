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
            Products.Select(p => p.In<Purchased, StoreUser>().Out<Purchased, StoreProduct>())
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
                p.In<Purchased, StoreUser>().Out<Purchased, StoreProduct>().Select(p => p.Name)
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

    [Test]
    public void ShouldFilterGraphTraversalNode()
    {
        string query = ToSurql(
            Products.Select(p =>
                p.In<Purchased, StoreUser>()
                    .Out<Purchased, StoreProduct>()
                    .Where(product => product.Price > 100)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::flatten($this<-purchased<-user->purchased->product[WHERE price > 100]) FROM product
                """
            );
    }

    [Test]
    public void ShouldFilterGraphTraversalEdge()
    {
        string query = ToSurql(
            Users.Select(u =>
                u.Out<Purchased, StoreProduct>().Where(step => step.Edge.Quantity > 1)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->(purchased WHERE quantity > 1)->product FROM user
                """
            );
    }

    [Test]
    public void ShouldProjectGraphTraversalEdgeMember()
    {
        string query = ToSurql(
            Users.Select(u => u.Out<Purchased, StoreProduct>().Select(step => step.Edge.Quantity))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->purchased.quantity FROM user
                """
            );
    }

    [Test]
    public void ShouldProjectGraphTraversalEdgeAndNodeMembers()
    {
        string query = ToSurql(
            Users.Select(u =>
                u.Out<Purchased, StoreProduct>()
                    .Select(step => new { step.Edge.Quantity, step.Node.Name })
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->purchased.{ Name: out.name, Quantity: quantity } FROM user
                """
            );
    }

    [Test]
    public void ShouldFlattenProjectedGraphTraversal()
    {
        string query = ToSurql(
            Users
                .SelectMany(user =>
                    user.Out<Purchased, StoreProduct>()
                        .Where(purchase => purchase.Edge.Quantity > 1)
                        .Select(purchase => new
                        {
                            purchase.Node.Name,
                            PurchasedSales = purchase.Edge.Quantity * purchase.Node.Price,
                        })
                )
                .OrderBy(purchase => purchase.PurchasedSales)
        );

        query
            .Should()
            .Be(
                """
                SELECT Name, PurchasedSales FROM array::flatten((SELECT VALUE $this->(purchased WHERE quantity > 1).{ Name: out.name, PurchasedSales: <float> quantity * out.price } FROM user)) ORDER BY PurchasedSales
                """
            );
    }
}
