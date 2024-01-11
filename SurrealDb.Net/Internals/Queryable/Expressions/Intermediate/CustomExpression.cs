using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

/// <summary>
/// A custom expression simply holding an inner expression.
/// Useful when mapping to a custom <see cref="Expressions.Surreal.ValueExpression"/>
/// </summary>
internal sealed class CustomExpression : IntermediateExpression
{
    public Expression Expression { get; }

    public CustomExpression(Expression expression, Type type)
        : base(type)
    {
        Expression = expression;
    }

    public Expression WithReturnType(Type returnType)
    {
        return new CustomExpression(Expression, returnType);
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Expression);
        return base.VisitChildren(visitor);
    }
}
