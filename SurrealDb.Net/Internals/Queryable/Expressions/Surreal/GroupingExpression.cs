using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class GroupingExpression : SurrealExpression
{
    public ImmutableArray<IdiomExpression> Fields { get; }

    public GroupingExpression(ImmutableArray<IdiomExpression> fields)
    {
        Fields = fields;
    }
}
