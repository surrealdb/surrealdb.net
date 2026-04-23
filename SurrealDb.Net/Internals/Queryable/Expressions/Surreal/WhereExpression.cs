namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class ConditionsExpression : SurrealExpression
{
    public ValueExpression Value { get; }

    public ConditionsExpression(ValueExpression value)
    {
        Value = value;
    }
}
