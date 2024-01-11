namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class SubqueryExpression : SurrealExpression
{
    public SelectExpression Select { get; }

    internal SubqueryExpression(Type type, SelectExpression select)
        : base(SurrealExpressionType.Aggregate)
    {
        Select = select;
    }
}
