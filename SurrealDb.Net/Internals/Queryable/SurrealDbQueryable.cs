using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace SurrealDb.Net.Internals.Queryable;

public interface ISurrealDbQueryable : IQueryable
{
    Type EnumerableElementType { get; }
    string FromTable { get; }
}

public sealed class SurrealDbQueryable<T>
    : ISurrealDbQueryable,
        IOrderedQueryable<T>,
        IAsyncEnumerable<T>
{
    public Type ElementType => typeof(T);
    public Type EnumerableElementType => typeof(IEnumerable<T>);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    private readonly string? _fromTable;
    public string FromTable =>
        string.IsNullOrWhiteSpace(_fromTable) ? GetTableFromElementType() : _fromTable;

    public SurrealDbQueryable(IQueryProvider provider, Expression expression)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public SurrealDbQueryable(IQueryProvider provider, string? fromTable)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = Expression.Constant(this);
        _fromTable = fromTable;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        if (Provider is IAsyncQueryProvider asyncProvider)
        {
            foreach (
                var item in await asyncProvider
                    .ExecuteAsync<IEnumerable<T>>(Expression, cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                yield return item;
            }
            yield break;
        }

        throw new InvalidOperationException(
            "The inner provider of IQueryable does not handle async"
        );
    }

    private string GetTableFromElementType()
    {
        var tableAttribute = ElementType.GetCustomAttribute<TableAttribute>();
        if (tableAttribute is not null)
        {
            var nameAttribute = tableAttribute.Name;
            if (!string.IsNullOrWhiteSpace(nameAttribute))
            {
                return nameAttribute;
            }
        }

        return ElementType.Name;
    }
}
