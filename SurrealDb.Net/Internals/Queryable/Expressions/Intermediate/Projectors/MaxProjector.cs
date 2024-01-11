namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class MaxProjector<T>
    where T : struct
{
    public T Max { get; set; }
}
