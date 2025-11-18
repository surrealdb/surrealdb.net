namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class SubqueryExpression : IntermediateExpression
{
    public SelectExpression Select { get; }

    internal SubqueryExpression(SelectExpression select, Type? type = null)
        : base(type ?? select.Type)
    {
        Select = select;
    }
}
