using System.Linq.Expressions;
using SurrealDb.Net.Models;

namespace SurrealDb.Net;

public interface IGraphTraversal;

public interface IGraphTraversal<out TNode> : IGraphTraversal, IEnumerable<TNode>
    where TNode : IRecord;

public interface IGraphTraversal<out TEdge, out TNode> : IGraphTraversal<TNode>
    where TEdge : IRelationRecord
    where TNode : IRecord;

public interface IGraphEdgeTraversal<out TEdge> : IGraphTraversal
    where TEdge : IRelationRecord;

public sealed class GraphStep<TEdge, TNode>
    where TEdge : IRelationRecord
    where TNode : IRecord
{
    public TEdge Edge { get; } = default!;

    public TNode Node { get; } = default!;
}

public static class GraphQueryableExtensions
{
    public static IGraphEdgeTraversal<TEdge> Out<TEdge>(this IRecord source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphEdgeTraversal<TEdge> Out<TEdge>(this IGraphTraversal source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

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

    public static IGraphTraversal<TEdge, TNode> Out<TEdge, TNode>(this IGraphTraversal source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphEdgeTraversal<TEdge> In<TEdge>(this IRecord source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphEdgeTraversal<TEdge> In<TEdge>(this IGraphTraversal source)
        where TEdge : IRelationRecord
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

    public static IGraphTraversal<TEdge, TNode> In<TEdge, TNode>(this IGraphTraversal source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IGraphEdgeTraversal<TEdge> Where<TEdge>(
        this IGraphEdgeTraversal<TEdge> source,
        Expression<Func<TEdge, bool>> predicate
    )
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    public static IEnumerable<TResult> Select<TEdge, TResult>(
        this IGraphEdgeTraversal<TEdge> source,
        Expression<Func<TEdge, TResult>> selector
    )
        where TEdge : IRelationRecord
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
