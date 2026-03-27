using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal abstract class OrderingExpression : SurrealExpression;

internal sealed class RandomOrderingExpression : OrderingExpression;

internal sealed class ListOrderingExpression : OrderingExpression
{
    public ImmutableArray<SurrealOrder> Orders { get; }

    public ListOrderingExpression(ImmutableArray<SurrealOrder> orders)
    {
        Orders = orders;
    }
}
