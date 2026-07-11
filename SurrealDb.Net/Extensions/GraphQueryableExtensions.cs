using System.Linq.Expressions;
using SurrealDb.Net.Models;

namespace SurrealDb.Net;

public interface IGraphTraversal<out TNode> : IEnumerable<TNode>
    where TNode : IRecord;

public interface IGraphTraversal<out TEdge, out TNode> : IGraphTraversal<TNode>
    where TEdge : IRelationRecord
    where TNode : IRecord;

public sealed class GraphStep<TEdge, TNode>
    where TEdge : IRelationRecord
    where TNode : IRecord
{
    public TEdge Edge { get; } = default!;

    public TNode Node { get; } = default!;
}

public static class GraphQueryableExtensions
{
    public static IGraphTraversal<TEdge, TNode> Out<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphTraversal<TEdge, TNode> Out<TEdge, TNode>(
        this IGraphTraversal<IRecord> source
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphTraversal<TEdge, TNode> In<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphTraversal<TEdge, TNode> In<TEdge, TNode>(
        this IGraphTraversal<IRecord> source
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphTraversal<TEdge, TNode> Where<TEdge, TNode>(
        this IGraphTraversal<TEdge, TNode> source,
        Expression<Func<TNode, bool>> predicate
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphTraversal<TEdge, TNode> Where<TEdge, TNode>(
        this IGraphTraversal<TEdge, TNode> source,
        Expression<Func<GraphStep<TEdge, TNode>, bool>> predicate
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IEnumerable<TResult> Select<TEdge, TNode, TResult>(
        this IGraphTraversal<TEdge, TNode> source,
        Expression<Func<TNode, TResult>> selector
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IEnumerable<TResult> Select<TEdge, TNode, TResult>(
        this IGraphTraversal<TEdge, TNode> source,
        Expression<Func<GraphStep<TEdge, TNode>, TResult>> selector
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }
}
