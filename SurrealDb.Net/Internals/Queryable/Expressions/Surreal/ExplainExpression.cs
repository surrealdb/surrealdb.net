namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class ExplainExpression : SurrealExpression
{
    public bool Full { get; }

    private ExplainExpression() { }
}
