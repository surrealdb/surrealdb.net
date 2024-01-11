using System.Collections.Immutable;
using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class OrdersExpression : IntermediateExpression
{
    public ImmutableArray<OrderByInfo> Infos { get; }

    public OrdersExpression(ImmutableArray<OrderByInfo> infos)
    {
        Infos = infos;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        foreach (var info in Infos)
        {
            visitor.Visit(info.Expression);
        }

        return this;
    }
}
