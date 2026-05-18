using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

/// <summary>
/// A custom expression simply holding an inner expression.
/// Useful when mapping to a custom <see cref="Expressions.Surreal.ValueExpression"/>
/// </summary>
internal sealed class CustomExpression : IntermediateExpression
{
    public Expression Expression { get; }
    public bool Flatten { get; }

    public CustomExpression(Expression expression, Type type, bool flatten = false)
        : base(type)
    {
        Expression = expression;
        Flatten = flatten;
    }

    public Expression WithReturnType(Type returnType)
    {
        return new CustomExpression(Expression, returnType, Flatten);
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Expression);
        return base.VisitChildren(visitor);
    }
}
