using SurrealDb.Net.Tests.Queryable.Models;

namespace SurrealDb.Net.Tests.Queryable;

public class ComplexQueryableTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSelectWithComplexQuery(string connectionString)
    {
        IEnumerable<string>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client
                .Select<Post>("post")
                .Where(p => p.Status == "DRAFT")
                .OrderBy(p => p.Id)
                .ThenBy(p => p.Title)
                .Skip(1)
                .Take(5)
                .Select(p => p.Content)
                .ToListAsync();
        };

        await func.Should().NotThrowAsync();

        // 2 (DRAFT) - (1 skipped) = 1
        result.Should().NotBeNull().And.HaveCount(1);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldGetFullOrdersSortedDescending(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client => client.Select<StoreOrder>().OrderByDescending(d => d.CreatedAt)
        );

        query
            .Should()
            .Be(
                "SELECT user.{created_at,email,id,name}, products.{created_at,description,id,name,price}, total, created_at, id FROM orders ORDER BY created_at DESC"
            );
        result.Should().NotBeNull().And.HaveCount(10);
        result.Should().BeInDescendingOrder(o => o.CreatedAt);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.Products.Should().NotBeEmpty();
                order
                    .Products.Should()
                    .AllSatisfy(product =>
                    {
                        product.Should().NotBeNull();
                        product.Id.Should().NotBeNull();
                        product.Name.Should().NotBeNullOrWhiteSpace();
                        product.Description.Should().NotBeNullOrWhiteSpace();
                        product.Price.Should().BeGreaterThan(0);
                        product.CreatedAt.Should().NotBeNull();
                    });
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldSumTotalOrderedProducts(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Store);

        var queryable = client.Select<StoreOrder>().Select(order => order.Products.Count());
        var query = queryable.ToQueryString();
        var result = await queryable.SumAsync();

        query.Should().Be("SELECT VALUE array::len(products) FROM orders");
        result.Should().Be(20);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldGetPartialOrders(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .Select(order => new StoreOrderSelector(
                        order.User.Name,
                        order.Total,
                        order
                            .Products.Select(product => new ProductSelector(
                                product.Name,
                                product.Description
                            ))
                            .ToArray()
                    ))
        );

        query
            .Should()
            .Be(
                "SELECT user.name AS customer, products.{description,name} AS products, total FROM orders"
            );
        result.Should().NotBeNull().And.HaveCount(10);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.products.Should().NotBeEmpty();
                order.total.Should().BeGreaterThan(0);
                order.customer.Should().NotBeNullOrWhiteSpace();
                order
                    .products.Should()
                    .AllSatisfy(product =>
                    {
                        product.Should().NotBeNull();
                        product.name.Should().NotBeNullOrWhiteSpace();
                        product.description.Should().NotBeNullOrWhiteSpace();
                    });
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldProjectComputedAndConstantFields(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .Select(order => new OrderMetaProjection(
                        order.Products.Any(),
                        order.Id,
                        order.Products.Count(),
                        "store"
                    ))
        );

        query
            .Should()
            .Be(
                "SELECT !array::is_empty(products) AS hasProducts, id, array::len(products) AS productCount, \"store\" AS source FROM orders"
            );
        result.Should().NotBeNull().And.HaveCount(10);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.hasProducts.Should().BeTrue();
                order.id.Should().NotBeNull();
                order.productCount.Should().BeGreaterThan(0);
                order.source.Should().Be("store");
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldGetFirstAndLastOrderDateForCustomer(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .Where(order =>
                        DateOnly.FromDateTime(order.CreatedAt ?? DateTime.MinValue)
                        >= DateOnly.FromDateTime(DateTime.MinValue)
                    )
                    .Select(order => new
                    {
                        OrderDate = DateOnly.FromDateTime(order.CreatedAt.GetValueOrDefault()),
                        OrderTime = TimeOnly.FromDateTime(order.CreatedAt.GetValueOrDefault()),
                    })
        );

        var firstOrderAt = result.Min(item => item.OrderDate);
        var lastOrderAt = result.Max(item => item.OrderDate);

        query
            .Should()
            .Be(
                "SELECT time::floor(created_at ?? d\"0001-01-01T00:00:00.0000000Z\", 1d) AS OrderDate, duration::from_nanos(time::nano(created_at ?? d\"0001-01-01T00:00:00.0000000Z\") % 86400000000000) AS OrderTime FROM orders WHERE time::floor(created_at ?? d\"0001-01-01T00:00:00.0000000Z\", 1d) >= time::floor(d\"0001-01-01T00:00:00.0000000Z\", 1d)"
            );
        firstOrderAt.Should().BeOnOrBefore(lastOrderAt);
        result.Should().OnlyContain(item => item.OrderTime >= TimeOnly.MinValue);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldFlattenOrderedProductsWithSelectMany(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client => client.Select<StoreOrder>().SelectMany(order => order.Products)
        );

        query
            .Should()
            .Be(
                "(SELECT array::flatten(products.{created_at,description,id,name,price}) AS Values FROM orders GROUP ALL)[0].Values"
            );
        result.Should().NotBeNull().And.HaveCount(20);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldSelectManyExpensiveProductsWithNameAndPriceOrderedAscending(
        string connectionString
    )
    {
        const float minPrice = 100;

        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .SelectMany(order => order.Products)
                    .Where(product => product.Price > minPrice)
                    .OrderBy(product => product.Price)
                    .Select(product => new { product.Name, product.Price })
        );

        query
            .Should()
            .Be(
                "(SELECT array::flatten(products.{created_at,description,id,name,price}) AS Values FROM orders GROUP ALL)[0].Values"
            );
        result.Should().NotBeNull().And.NotBeEmpty();
        result.Should().OnlyContain(product => product.Price > minPrice);
        result.Should().BeInAscendingOrder(product => product.Price);
        result.Should().OnlyContain(product => !string.IsNullOrWhiteSpace(product.Name));
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldSplitQueryWhenOrderingByFieldOutsideProjection(string connectionString)
    {
        var minTotal = 200;
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .OrderByDescending(order => order.CreatedAt)
                    .Where(order => order.Total > minTotal)
                    .Select(order => new OrderedOrderProjection(
                        order.Id,
                        order.User.Name,
                        order.Total
                    ))
        );

        query
            .Should()
            .Be(
                "SELECT user.name AS customer, id, total FROM (SELECT user.name, id, total, created_at FROM orders WHERE total > <float> $minTotal ORDER BY created_at DESC)"
            );
        result.Should().NotBeNull().And.HaveCount(3);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.id.Should().NotBeNull();
                order.customer.Should().NotBeNullOrWhiteSpace();
                order.total.Should().BeGreaterThan(minTotal);
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldRoundSumProductCosts(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .Select(order => new
                    {
                        Total = order.Total,
                        ProductCosts = (float)
                            Math.Round(order.Products.Sum(product => product.Price), 2),
                    })
        );

        query
            .Should()
            .Be(
                "SELECT <float> math::fixed(<float> math::sum(products.price), 2) AS ProductCosts, total AS Total FROM orders"
            );
        result.Should().NotBeEmpty().And.HaveCount(10);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.Total.Should().Be(order.ProductCosts);
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldGetTopOrdersByTotal(string connectionString)
    {
        const int top = 3;
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .OrderByDescending(order => order.Total)
                    .Take(top)
                    .Select(order => new OrderedOrderProjection(
                        order.Id,
                        order.User.Name,
                        order.Total
                    ))
        );

        query
            .Should()
            .Be("SELECT user.name AS customer, id, total FROM orders ORDER BY total DESC LIMIT 3");
        result.Should().NotBeNull().And.HaveCount(top);
        result.Should().BeInDescendingOrder(order => order.total);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.id.Should().NotBeNull();
                order.customer.Should().NotBeNullOrWhiteSpace();
                order.total.Should().BeGreaterThan(0);
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldRoundToIntSumProductCosts(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .Select(order => new
                    {
                        Total = (float)Math.Round(order.Total),
                        ProductCosts = (float)
                            Math.Round(order.Products.Sum(product => product.Price)),
                    })
        );

        query
            .Should()
            .Be(
                "SELECT <float> math::round(<float> math::sum(products.price)) AS ProductCosts, <float> math::round(<float> total) AS Total FROM orders"
            );
        result.Should().NotBeEmpty().And.HaveCount(10);
        result
            .Should()
            .AllSatisfy(order =>
            {
                order.Total.Should().Be(order.ProductCosts);
                ((int)order.Total).Should().Be((int)order.ProductCosts);
            });
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    public async Task ShouldSumAllProductCosts(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Store);
        var queryable = client
            .Select<StoreOrder>()
            .Select(order => order.Products.Sum(product => product.Price));
        var query = queryable.ToQueryString();
        var result = await queryable.SumAsync();

        // Due to optimizeSelfProjection the query is not exactly what we would expect, but it should still work correctly
        // math::sum(SELECT VALUE math::sum(products.price) FROM orders)
        query.Should().Be("SELECT VALUE math::sum(products.price) FROM orders");
        result.Should().BeApproximately(3879.8f, 0.1f);
    }

    private record StoreOrderSelector(string customer, float total, ProductSelector[] products);

    private record ProductSelector(string name, string description);

    private record OrderMetaProjection(
        bool hasProducts,
        RecordId? id,
        int productCount,
        string source
    );

    private record OrderedOrderProjection(RecordId? id, string customer, float total);

    private static async Task<(List<T> Result, string Query)> ExecuteWithSchema<T>(
        string connectionString,
        SurrealSchemaFile schema,
        Func<SurrealDbClient, IQueryable<T>> queryFactory
    )
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(schema);

        var query = queryFactory(client);
        var queryString = query.ToQueryString();
        var result = await query.ToListAsync();

        return (result, queryString);
    }
}
