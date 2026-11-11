namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class VersionExpression : SurrealExpression
{
    public ValueExpression Value { get; }

    private VersionExpression(ValueExpression value)
    {
        Value = value;
    }
}
