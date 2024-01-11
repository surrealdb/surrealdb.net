using System.Collections;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly IEnumerable<TElement> _group;

    public TKey Key { get; }

    public Grouping(TKey key, IEnumerable<TElement> group)
    {
        Key = key;
        _group = group;
    }

    public IEnumerator<TElement> GetEnumerator()
    {
        return _group.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _group.GetEnumerator();
    }
}
