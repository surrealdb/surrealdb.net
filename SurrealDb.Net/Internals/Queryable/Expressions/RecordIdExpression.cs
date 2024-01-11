namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class RecordIdExpression : SurrealExpression
{
    public string Name { get; }

    internal RecordIdExpression(string name)
        : base(SurrealExpressionType.RecordId)
    {
        Name = name;
    }
}
