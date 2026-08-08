using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class OrderByInfo
{
    public OrderType OrderType { get; }
    public Expression Expression { get; }

    public OrderByInfo(OrderType orderType, Expression expression)
    {
        OrderType = orderType;
        Expression = expression;
    }
}
