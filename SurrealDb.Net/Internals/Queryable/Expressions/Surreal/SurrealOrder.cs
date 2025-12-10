namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class SurrealOrder
{
    public IdiomExpression Value { get; }
    public bool Collate { get; }
    public bool Numeric { get; }

    /// <summary>
    /// <c>true</c> if the direction is ascending
    /// </summary>
    public bool Direction { get; }

    public SurrealOrder(IdiomExpression value, Intermediate.OrderType orderType)
    {
        Value = value;
        Direction = orderType == Intermediate.OrderType.Ascending;
    }

    public string GetSuffix()
    {
        if (Collate)
        {
            return " COLLATE";
        }
        if (Numeric)
        {
            return " NUMERIC";
        }
        if (!Direction)
        {
            return " DESC";
        }
        return string.Empty;
    }
}
