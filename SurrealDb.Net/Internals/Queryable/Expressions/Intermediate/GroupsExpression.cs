using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class GroupsExpression : IntermediateExpression
{
    public Expression? Expression { get; }

    public GroupsExpression(Expression? expression)
    {
        Expression = expression;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Expression);
        return this;
    }
}
