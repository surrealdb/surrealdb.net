namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class FlattenProjector<T>
{
    public T[] Values { get; set; } = [];
}
