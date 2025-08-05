using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal abstract class IntermediateExpression : Expression
{
    public override Type Type { get; }

    public override ExpressionType NodeType { get; }

    protected IntermediateExpression()
    {
        Type = null!;
    }

    protected IntermediateExpression(Type type)
    {
        Type = type;
    }
}
