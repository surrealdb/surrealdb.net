using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// Represents an expression associated with a query plan operator,
/// as returned by <c>EXPLAIN</c> or <c>EXPLAIN FULL</c>.
/// </summary>
public sealed class ExplainPlanExpression
{
    /// <summary>
    /// The role of this expression within the operator (e.g. <c>condition</c>, <c>projection</c>).
    /// </summary>
    [CborProperty("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The SurrealQL representation of the expression.
    /// </summary>
    [CborProperty("sql")]
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Sub-plans embedded within this expression (e.g. correlated subqueries).
    /// Only present when the expression contains nested operators.
    /// </summary>
    [CborProperty("embedded_operators")]
    public List<ExplainEmbeddedOperator>? EmbeddedOperators { get; set; }

    internal ExplainPlanExpression() { }
}
