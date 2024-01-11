using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal sealed class GroupByInfo
{
    public string Alias { get; }
    public Expression Element { get; }

    internal GroupByInfo(string alias, Expression element)
    {
        Alias = alias;
        Element = element;
    }
}
