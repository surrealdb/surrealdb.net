namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class LimitExpression : SurrealExpression
{
    public ValueExpression Value { get; }

    public LimitExpression(ValueExpression value)
    {
        Value = value;
    }
}
