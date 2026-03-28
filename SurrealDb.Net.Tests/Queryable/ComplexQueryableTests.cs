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
    public async Task ShouldGetFullOrdersObjectSortedDescending(string connectionString)
    {
        var (result, query) = await ExecuteWithSchema(
            connectionString,
            SurrealSchemaFile.Store,
            client =>
                client
                    .Select<StoreOrder>()
                    .Select(order => new
                    {
                        Id = order.Id,
                        CreatedAt = order.CreatedAt,
                        User = order.User,
                        Total = order.Total,
                        Products = order
                            .Products.Select(product => new
                            {
                                Id = product.Id,
                                Name = product.Name,
                                Description = product.Description,
                                Price = product.Price,
                                CreatedAt = product.CreatedAt,
                            })
                            .ToArray(),
                    })
                    .OrderByDescending(d => d.CreatedAt)
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
