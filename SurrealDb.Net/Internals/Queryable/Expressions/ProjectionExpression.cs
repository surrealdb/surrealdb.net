using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class ProjectionExpression : SurrealExpression
{
    public SelectExpression Source { get; }
    public Expression Projector { get; }

    internal ProjectionExpression(SelectExpression source, Expression projector)
        : base(SurrealExpressionType.Projection)
    {
        Source = source;
        Projector = projector;
    }
}
