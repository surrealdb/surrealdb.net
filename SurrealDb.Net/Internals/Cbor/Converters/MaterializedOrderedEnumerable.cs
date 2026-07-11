namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Preserves an ordering that was already applied by SurrealDB.
/// </summary>
internal sealed class MaterializedOrderedEnumerable<T> : IOrderedEnumerable<T>
{
    private readonly IReadOnlyList<T> _items;

    public MaterializedOrderedEnumerable(IEnumerable<T> items)
    {
        _items = items.ToArray();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(
        Func<T, TKey> keySelector,
        IComparer<TKey>? comparer,
        bool descending
    )
    {
        // The server result is already totally ordered. Its materialized ordinal is the
        // primary key, so there are no equal primary keys on which ThenBy could operate.
        return this;
    }
}
