using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class SkipExpression : IntermediateExpression
{
    public Expression Expression { get; }

    public SkipExpression(Expression expression)
    {
        Expression = expression;
    }
}
