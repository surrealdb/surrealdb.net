using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// Runtime execution metrics for a single operator in an EXPLAIN FULL query plan.
/// </summary>
public sealed class ExplainMetrics
{
    /// <summary>
    /// The number of rows produced by this operator.
    /// </summary>
    [CborProperty("output_rows")]
    public long OutputRows { get; set; }

    /// <summary>
    /// The number of batches produced by this operator.
    /// </summary>
    [CborProperty("output_batches")]
    public long OutputBatches { get; set; }

    /// <summary>
    /// The elapsed time for this operator in nanoseconds.
    /// </summary>
    [CborProperty("elapsed_ns")]
    public long ElapsedNs { get; set; }

    internal ExplainMetrics() { }
}
