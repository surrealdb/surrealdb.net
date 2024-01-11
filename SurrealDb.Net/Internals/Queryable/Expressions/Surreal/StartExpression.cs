namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class StartExpression : SurrealExpression
{
    public ValueExpression Value { get; }

    public StartExpression(ValueExpression value)
    {
        Value = value;
    }
}
