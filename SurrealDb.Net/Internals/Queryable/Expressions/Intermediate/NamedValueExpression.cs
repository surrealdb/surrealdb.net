using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class NamedValueExpression : IntermediateExpression
{
    public Type ReturnType { get; }
    public string Name { get; }
    public object? Value { get; }

    public NamedValueExpression(Type returnType, string name, object? value)
        : base(returnType)
    {
        ReturnType = returnType;
        Name = name;
        Value = value;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        return this;
    }
}
