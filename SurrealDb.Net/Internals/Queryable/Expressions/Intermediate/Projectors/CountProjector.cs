namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class CountProjector<T>
    where T : struct
{
    public T count { get; set; }
}
