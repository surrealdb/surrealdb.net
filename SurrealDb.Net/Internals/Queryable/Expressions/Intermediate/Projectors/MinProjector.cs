namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class MinProjector<T>
    where T : struct
{
    public T Min { get; set; }
}
