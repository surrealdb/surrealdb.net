using SurrealDb.Net.Models;

namespace SurrealDb.Net;

public static class GraphQueryableExtensions
{
    public static IEnumerable<TNode> Out<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IEnumerable<TNode> In<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }
}
