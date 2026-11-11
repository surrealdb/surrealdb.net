using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// Represents an operator plan embedded inside an expression,
/// as returned by <c>EXPLAIN</c> or <c>EXPLAIN FULL</c>.
/// </summary>
public sealed class ExplainEmbeddedOperator
{
    /// <summary>
    /// The role this embedded operator plays within its parent expression.
    /// </summary>
    [CborProperty("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The nested execution plan for this embedded operator.
    /// </summary>
    [CborProperty("plan")]
    public ExplainPlan Plan { get; set; } = null!;

    internal ExplainEmbeddedOperator() { }
}
