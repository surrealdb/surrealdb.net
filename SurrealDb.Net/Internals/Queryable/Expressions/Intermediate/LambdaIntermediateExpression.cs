using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class LambdaIntermediateExpression : IntermediateExpression
{
    public LambdaExpression Expression { get; }

    public LambdaIntermediateExpression(LambdaExpression expression)
        : base(expression.Type)
    {
        Expression = expression;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        foreach (var parameter in Expression.Parameters)
        {
            visitor.Visit(parameter);
        }

        visitor.Visit(Expression.Body);

        return this;
    }
}
