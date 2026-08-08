using System.Linq.Expressions;
using SurrealDb.Net.Models;

namespace SurrealDb.Net;

/// <summary>
/// Marks an expression as a graph traversal within a SurrealDB LINQ query.
/// </summary>
public interface IGraphTraversal;

/// <summary>
/// Represents a graph traversal whose current nodes are of type <typeparamref name="TNode"/>.
/// </summary>
/// <typeparam name="TNode">The type of node reached by the traversal.</typeparam>
public interface IGraphTraversal<out TNode> : IGraphTraversal, IEnumerable<TNode>
    where TNode : IRecord;

/// <summary>
/// Represents a graph traversal from edges of type <typeparamref name="TEdge"/> to nodes of type
/// <typeparamref name="TNode"/>.
/// </summary>
/// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
/// <typeparam name="TNode">The node record type reached by the graph step.</typeparam>
public interface IGraphTraversal<out TEdge, out TNode> : IGraphTraversal<TNode>
    where TEdge : IRelationRecord
    where TNode : IRecord;

/// <summary>
/// Represents a graph traversal over edges of type <typeparamref name="TEdge"/> for filtering or
/// projecting relation fields.
/// </summary>
/// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
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
    /// <summary>Traverses outgoing <typeparamref name="TEdge"/> edges and exposes their edge records.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    public static IGraphEdgeTraversal<TEdge> Out<TEdge>(this IRecord source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues an outgoing edge traversal and exposes its edge records.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    public static IGraphEdgeTraversal<TEdge> Out<TEdge>(this IGraphTraversal source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Traverses outgoing <typeparamref name="TEdge"/> edges to typed nodes.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> Out<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues an outgoing edge traversal to typed nodes.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
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

    /// <summary>Continues an outgoing graph traversal to typed nodes.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> Out<TEdge, TNode>(this IGraphTraversal source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Traverses incoming <typeparamref name="TEdge"/> edges and exposes their edge records.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    public static IGraphEdgeTraversal<TEdge> In<TEdge>(this IRecord source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues an incoming edge traversal and exposes its edge records.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    public static IGraphEdgeTraversal<TEdge> In<TEdge>(this IGraphTraversal source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Traverses incoming <typeparamref name="TEdge"/> edges to typed nodes.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> In<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues an incoming edge traversal to typed nodes.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
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

    /// <summary>Continues an incoming graph traversal to typed nodes.</summary>
    /// <typeparam name="TEdge">The relation record type to traverse.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> In<TEdge, TNode>(this IGraphTraversal source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Traverses both directions of <typeparamref name="TEdge"/> and exposes edge records.</summary>
    /// <typeparam name="TEdge">A relation whose endpoints have the same node type.</typeparam>
    public static IGraphEdgeTraversal<TEdge> Both<TEdge>(this IRecord source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues a bidirectional edge traversal and exposes its edge records.</summary>
    /// <typeparam name="TEdge">A relation whose endpoints have the same node type.</typeparam>
    public static IGraphEdgeTraversal<TEdge> Both<TEdge>(this IGraphTraversal source)
        where TEdge : IRelationRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Traverses both directions of <typeparamref name="TEdge"/> to typed nodes.</summary>
    /// <typeparam name="TEdge">A relation whose endpoints have the same node type.</typeparam>
    /// <typeparam name="TNode">The shared node type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> Both<TEdge, TNode>(this IRecord source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues a bidirectional edge traversal to typed nodes.</summary>
    /// <typeparam name="TEdge">A relation whose endpoints have the same node type.</typeparam>
    /// <typeparam name="TNode">The shared node type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> Both<TEdge, TNode>(
        this IGraphTraversal<IRecord> source
    )
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Continues a bidirectional graph traversal to typed nodes.</summary>
    /// <typeparam name="TEdge">A relation whose endpoints have the same node type.</typeparam>
    /// <typeparam name="TNode">The shared node type reached by the traversal.</typeparam>
    public static IGraphTraversal<TEdge, TNode> Both<TEdge, TNode>(this IGraphTraversal source)
        where TEdge : IRelationRecord
        where TNode : IRecord
    {
        throw new NotSupportedException(
            "Graph traversal methods are only supported in LINQ queries."
        );
    }

    /// <summary>Filters the relation records of an edge traversal.</summary>
    /// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
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

    /// <summary>Projects fields from the relation records of an edge traversal.</summary>
    /// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
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

    /// <summary>Filters the nodes reached by a typed graph traversal.</summary>
    /// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
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

    /// <summary>
    /// Filters a typed graph traversal with access to both its relation edge and endpoint node.
    /// </summary>
    /// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
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

    /// <summary>Projects fields from the nodes reached by a typed graph traversal.</summary>
    /// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
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

    /// <summary>
    /// Projects a typed graph traversal with access to both its relation edge and endpoint node.
    /// </summary>
    /// <typeparam name="TEdge">The relation record type traversed by the graph step.</typeparam>
    /// <typeparam name="TNode">The node record type reached by the traversal.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
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
