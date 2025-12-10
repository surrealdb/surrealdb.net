using System.Collections;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class SurrealGrouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    public TKey Key { get; }
    public IEnumerable<TElement> Values { get; }

    public SurrealGrouping(TKey key, IEnumerable<TElement> values)
    {
        Key = key;
        Values = values;
    }

    public IEnumerator<TElement> GetEnumerator()
    {
        return Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Values.GetEnumerator();
    }
}
