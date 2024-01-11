using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class AggregateExpression : SurrealExpression
{
    public AggregateType AggregateType { get; }
    public Expression? Argument { get; }

    internal AggregateExpression(Type type, AggregateType aggregateType, Expression? argument)
        : base(SurrealExpressionType.Aggregate)
    {
        AggregateType = aggregateType;
        Argument = argument;
    }
}
