using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class AggregateSubqueryExpression : SurrealExpression
{
    public string GroupByAlias { get; }
    public Expression AggregateInGroupSelect { get; }
    public SubqueryExpression AggregateAsSubquery { get; }

    internal AggregateSubqueryExpression(
        string groupByAlias,
        Expression aggregateInGroupSelect,
        SubqueryExpression aggregateAsSubquery
    )
        : base(SurrealExpressionType.AggregateSubquery)
    {
        GroupByAlias = groupByAlias;
        AggregateInGroupSelect = aggregateInGroupSelect;
        AggregateAsSubquery = aggregateAsSubquery;
    }
}
