namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class ColumnExpression : SurrealExpression
{
    public string Alias { get; }
    public string Name { get; }

    internal ColumnExpression(Type type, string alias, string name)
        : base(SurrealExpressionType.Column)
    {
        Alias = alias;
        Name = name;
    }
}
