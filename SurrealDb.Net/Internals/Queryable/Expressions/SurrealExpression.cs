using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal abstract class SurrealExpression : Expression
{
    public SurrealExpressionType SurrealNodeType { get; }
    public override Type Type { get; }

    protected SurrealExpression(SurrealExpressionType surrealNodeType)
    {
        Type = null!; // TODO : ?
        SurrealNodeType = surrealNodeType;
    }

    protected SurrealExpression(Type type, SurrealExpressionType surrealNodeType)
    {
        Type = type;
        SurrealNodeType = surrealNodeType;
    }
}
