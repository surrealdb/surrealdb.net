namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class TableExpression : SurrealExpression
{
    public string Alias { get; }
    public string Name { get; }

    internal TableExpression(Type type, string alias, string name)
        : base(SurrealExpressionType.Table)
    {
        Alias = alias;
        Name = name;
    }
}
