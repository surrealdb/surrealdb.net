namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class ExplainExpression : SurrealExpression
{
    public bool Full { get; }

    public ExplainExpression(bool full)
    {
        Full = full;
    }
}
