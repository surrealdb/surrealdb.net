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

    [Test]
    public void ShouldFilterIncomingGraphTraversalEdge()
    {
        string query = ToSurql(
            Products.Select(product =>
                product.In<Purchased, StoreUser>().Where(step => step.Edge.Quantity > 1)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this<-(purchased WHERE quantity > 1)<-user FROM product
                """
            );
    }

    [Test]
    public void ShouldProjectIncomingGraphTraversalEdgeAndNodeMembers()
    {
        string query = ToSurql(
            Products.Select(product =>
                product
                    .In<Purchased, StoreUser>()
                    .Select(step => new { step.Edge.Quantity, step.Node.Name })
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this<-purchased.{ Name: in.name, Quantity: quantity } FROM product
                """
            );
    }

    [Test]
    public void ShouldCombineSuccessiveGraphTraversalEdgeFilters()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>()
                    .Where(step => step.Edge.Quantity > 1)
                    .Where(step => step.Edge.PurchasedAt != null)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->(purchased WHERE quantity > 1 && purchased_at != null)->product FROM user
                """
            );
    }

    [Test]
    public void ShouldApplySuccessiveGraphTraversalNodeFilters()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>()
                    .Where(product => product.Price > 100)
                    .Where(product => product.Name.StartsWith("M"))
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->purchased->product[WHERE price > 100][WHERE string::starts_with(name, "M")] FROM user
                """
            );
    }

    [Test]
    public void ShouldFilterLastEdgeInChainedGraphTraversal()
    {
        string query = ToSurql(
            Products.Select(product =>
                product
                    .In<Purchased, StoreUser>()
                    .Out<Purchased, StoreProduct>()
                    .Where(step => step.Edge.Quantity > 1)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::flatten($this<-purchased<-user->(purchased WHERE quantity > 1)->product) FROM product
                """
            );
    }

    [Test]
    public void ShouldFilterGraphTraversalEdgeAndNodeTogether()
    {
        int minQuantity = 2;
        float minPrice = 100;

        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>()
                    .Where(step => step.Edge.Quantity >= minQuantity && step.Node.Price >= minPrice)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this->(purchased WHERE quantity >= $minQuantity && out.price >= $minPrice)->product FROM user
                """
            );
        Parameters.Should().ContainKey("minQuantity").WhoseValue.Should().Be(minQuantity);
        Parameters.Should().ContainKey("minPrice").WhoseValue.Should().Be(minPrice);
    }

    [Test]
    public void ShouldFilterIncomingGraphTraversalEdgeAndNodeTogether()
    {
        string query = ToSurql(
            Products.Select(product =>
                product
                    .In<Purchased, StoreUser>()
                    .Where(step => step.Edge.Quantity > 1 && step.Node.Name.StartsWith("D"))
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE $this<-(purchased WHERE quantity > 1 && string::starts_with(in.name, "D"))<-user FROM product
                """
            );
    }

    [Test]
    public void ShouldFilterRootByGraphTraversalAny()
    {
        string query = ToSurql(
            Users
                .Where(user => user.Out<Purchased, StoreProduct>().Any())
                .Select(user => user.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE !array::is_empty($this->purchased->product)
                """
            );
    }

    [Test]
    public void ShouldApplyAnyPredicateToGraphTraversal()
    {
        string query = ToSurql(
            Users
                .Where(user =>
                    user.Out<Purchased, StoreProduct>().Any(product => product.Price > 100)
                )
                .Select(user => user.Username)
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE Username FROM user WHERE !array::is_empty($this->purchased->product[WHERE price > 100])
                """
            );
    }

    [Test]
    public void ShouldCountFilteredGraphTraversalNodes()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>().Count(product => product.Price > 100)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::len($this->purchased->product[WHERE price > 100]) FROM user
                """
            );
    }

    [Test]
    public void ShouldCountGraphTraversalNodes()
    {
        string query = ToSurql(Users.Select(user => user.Out<Purchased, StoreProduct>().Count()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::len($this->purchased->product) FROM user
                """
            );
    }

    [Test]
    public void ShouldProjectDistinctGraphTraversalMember()
    {
        string query = ToSurql(
            Products.Select(product =>
                product.In<Purchased, StoreUser>().Select(user => user.Name).Distinct()
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::distinct($this<-purchased<-user.name) FROM product
                """
            );
    }

    [Test]
    public void ShouldOrderAndPageGraphTraversal()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>()
                    .OrderByDescending(product => product.Price)
                    .ThenBy(product => product.Name)
                    .Skip(1)
                    .Take(2)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::slice(array::slice((SELECT VALUE $this FROM $this->purchased->product ORDER BY price DESC, name), 1), 0, 2) FROM user
                """
            );
    }

    [Test]
    public void ShouldOrderGraphTraversalByMultipleDirections()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>()
                    .OrderBy(product => product.Price)
                    .ThenByDescending(product => product.Name)
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE (SELECT VALUE $this FROM $this->purchased->product ORDER BY price, name DESC) FROM user
                """
            );
    }

    [Test]
    public void ShouldProjectFirstGraphTraversalNodeOrDefault()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>()
                    .OrderBy(product => product.Price)
                    .FirstOrDefault()
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE array::first((SELECT VALUE $this FROM $this->purchased->product ORDER BY price)) FROM user
                """
            );
    }

    [Test]
    public void ShouldAggregateProjectedGraphTraversalNodeMembers()
    {
        string sumQuery = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>().Select(product => product.Price).Sum()
            )
        );
        string averageQuery = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>().Select(product => product.Price).Average()
            )
        );
        string minQuery = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>().Select(product => product.Price).Min()
            )
        );
        string maxQuery = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>().Select(product => product.Price).Max()
            )
        );

        sumQuery.Should().Be("SELECT VALUE math::sum($this->purchased->product.price) FROM user");
        averageQuery
            .Should()
            .Be("SELECT VALUE math::mean($this->purchased->product.price) FROM user");
        minQuery.Should().Be("SELECT VALUE array::min($this->purchased->product.price) FROM user");
        maxQuery.Should().Be("SELECT VALUE array::max($this->purchased->product.price) FROM user");
    }

    [Test]
    public void ShouldSumProjectedGraphTraversalEdgeMember()
    {
        string query = ToSurql(
            Users.Select(user =>
                user.Out<Purchased, StoreProduct>().Select(step => step.Edge.Quantity).Sum()
            )
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE math::sum($this->purchased.quantity) FROM user
                """
            );
    }
}
