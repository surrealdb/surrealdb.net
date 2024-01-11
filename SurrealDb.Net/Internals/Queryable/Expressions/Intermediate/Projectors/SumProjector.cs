namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

internal sealed class SumProjector<T>
    where T : struct
{
    public T Sum { get; set; }
}
