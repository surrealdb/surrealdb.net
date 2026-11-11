namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class AvgProjector<T>
    where T : struct
{
    public T Avg { get; set; }
}
