using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class WhereExpression : IntermediateExpression
{
    public Expression Expression { get; }

    public WhereExpression(Expression expression)
    {
        Expression = expression;
    }
}
