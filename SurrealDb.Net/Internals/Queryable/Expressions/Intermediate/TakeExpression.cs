using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class TakeExpression : IntermediateExpression
{
    public Expression Expression { get; }

    public TakeExpression(Expression expression)
    {
        Expression = expression;
    }
}
