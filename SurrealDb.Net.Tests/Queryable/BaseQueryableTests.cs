using System.Linq.Expressions;
using System.Reactive;
using SurrealDb.Net.Internals.Queryable;

namespace SurrealDb.Net.Tests.Queryable;

public abstract class BaseQueryableTests
{
    private const string PostTableName = "post";
    private const string UserTableName = "user";
    private const string AddressTableName = "address";
    private const string OrderTableName = "order";
    private const string ProductTableName = "product";

    private readonly Lazy<IQueryable<Post>> _lazyPosts = new(CreateQueryable<Post>(PostTableName));
    private readonly Lazy<IQueryable<Models.User>> _lazyUsers = new(
        CreateQueryable<Models.User>(UserTableName)
    );
    private readonly Lazy<IQueryable<Models.Address>> _lazyAddresses = new(
        CreateQueryable<Models.Address>(AddressTableName)
    );
    private readonly Lazy<IQueryable<Models.Order>> _lazyOrders = new(
        CreateQueryable<Models.Order>(OrderTableName)
    );
    private readonly Lazy<IQueryable<Models.Product>> _lazyProducts = new(
        CreateQueryable<Models.Product>(ProductTableName)
    );

    protected IQueryable<Post> Posts => _lazyPosts.Value;
    protected IQueryable<Models.User> Users => _lazyUsers.Value;
    protected IQueryable<Models.Address> Addresses => _lazyAddresses.Value;
    protected IQueryable<Models.Order> Orders => _lazyOrders.Value;
    protected IQueryable<Models.Product> Products => _lazyProducts.Value;

    protected IReadOnlyDictionary<string, object?> Parameters { get; private set; } =
        new Dictionary<string, object?>();

    protected string ToSurql<T>(IQueryable<T> queryable)
    {
        var tableName = queryable.Provider switch
        {
            { } qp when qp == Posts.Provider => PostTableName,
            { } qp when qp == Users.Provider => UserTableName,
            { } qp when qp == Addresses.Provider => AddressTableName,
            { } qp when qp == Orders.Provider => OrderTableName,
            { } qp when qp == Products.Provider => ProductTableName,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new InvalidOperationException("Invalid table name");
        }

        return ToSurql(queryable.Expression);
    }

    protected string ToSurql(Expression expression)
    {
        var (query, parameters) = new SurrealDbQueryProvider<Unit>(null!).Translate(expression);
        Parameters = parameters;

        return query;
    }

    private static SurrealDbQueryable<T> CreateQueryable<T>(string table)
    {
        return new(new SurrealDbQueryProvider<T>(null!), table);
    }
}
