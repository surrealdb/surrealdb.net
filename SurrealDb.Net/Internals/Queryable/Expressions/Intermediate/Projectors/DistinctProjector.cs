namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class DistinctProjector<T>
{
    public T[] Values { get; set; } = [];
}
