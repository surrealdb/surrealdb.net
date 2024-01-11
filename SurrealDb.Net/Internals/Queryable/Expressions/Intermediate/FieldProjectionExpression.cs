using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class FieldProjectionExpression : IntermediateExpression
{
    public Expression? Expression { get; }
    public string? Alias { get; }

    public FieldProjectionExpression(
        Type resultType,
        Expression? expression = null,
        string? alias = null
    )
        : base(resultType)
    {
        Expression = expression;
        Alias = alias;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Expression);
        return this;
    }
}
