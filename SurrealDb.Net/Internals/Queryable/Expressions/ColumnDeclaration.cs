using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class ColumnDeclaration
{
    public string Name { get; }
    public Expression Expression { get; }

    internal ColumnDeclaration(string name, Expression expression)
    {
        Name = name;
        Expression = expression;
    }
}
