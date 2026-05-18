using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// Represents the execution plan returned by a <c>SELECT ... EXPLAIN</c> or
/// <c>SELECT ... EXPLAIN FULL</c> query.
/// Each node describes a single operator in the query plan tree.
/// </summary>
public sealed class ExplainPlan
{
    /// <summary>
    /// The name of the query operator (e.g. <c>SelectProject</c>, <c>TableScan</c>, <c>IndexScan</c>).
    /// </summary>
    [CborProperty("operator")]
    public string? Operator { get; set; }

    /// <summary>
    /// The execution context required by this operator (e.g. <c>Db</c>, <c>Root</c>).
    /// </summary>
    [CborProperty("context")]
    public string? Context { get; set; }

    /// <summary>
    /// Additional operator-specific attributes (e.g. table name, index name, direction).
    /// </summary>
    [CborProperty("attributes")]
    public Dictionary<string, object?>? Attributes { get; set; }

    /// <summary>
    /// Child operators in the plan tree.
    /// </summary>
    [CborProperty("children")]
    public List<ExplainPlan>? Children { get; set; }

    /// <summary>
    /// Expressions associated with this operator, including their SurrealQL text
    /// and any embedded sub-plans.
    /// </summary>
    [CborProperty("expressions")]
    public List<ExplainPlanExpression>? Expressions { get; set; }

    /// <summary>
    /// Runtime execution metrics. Only populated when using <c>EXPLAIN FULL</c>.
    /// </summary>
    [CborProperty("metrics")]
    public ExplainMetrics? Metrics { get; set; }

    /// <summary>
    /// Total number of rows returned by the query. Only present on the root node
    /// when using <c>EXPLAIN FULL</c>.
    /// </summary>
    [CborProperty("total_rows")]
    public long? TotalRows { get; set; }

    internal ExplainPlan() { }
}
