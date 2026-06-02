using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal abstract class IndexingExpression : SurrealExpression;

internal sealed class NoIndexExpression : IndexingExpression;

internal sealed class WithIndexesExpression : IndexingExpression
{
    public ImmutableArray<string> Names { get; }

    private WithIndexesExpression() { }
}
