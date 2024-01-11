namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal enum SurrealExpressionType
{
    Table = 1,
    RecordId,
    Column,
    Select,
    Projection,
    Aggregate,
    Subquery,
    AggregateSubquery,
}
