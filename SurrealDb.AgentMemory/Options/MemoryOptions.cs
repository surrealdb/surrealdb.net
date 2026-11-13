using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// The temporal filters the fact reads accept, as RFC 3339 timestamps.
/// </summary>
public class TemporalOptions
{
    /// <summary>Known time: read the facts as they stood at this instant.</summary>
    public string? AsOf { get; set; }

    /// <summary>Read through MVCC at this instant.</summary>
    public string? AtInstant { get; set; }

    /// <summary>World-time lower bound.</summary>
    public string? ValidFrom { get; set; }

    /// <summary>World-time upper bound.</summary>
    public string? ValidUntil { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.RememberAsync"/>.
/// </summary>
public sealed class RememberOptions
{
    /// <summary>Inference mode (see <see cref="InferModes"/>). <c>full</c> is the default.</summary>
    public string? Infer { get; set; }

    /// <summary>Existing session to attach the turn to (auto-created when absent).</summary>
    public string? SessionId { get; set; }

    /// <summary>DNF scope selector the write targets (outer OR, inner AND).</summary>
    public Scope? Scopes { get; set; }

    /// <summary>Role to record on the turn (see <see cref="TurnRoles"/>). <c>user</c> by default.</summary>
    public string? Role { get; set; }

    /// <summary>Overrides the memory category for extracted and triple facts (see <see cref="MemoryCategories"/>).</summary>
    public string? MemoryCategory { get; set; }

    /// <summary>Descriptive <c>key=value</c> labels for the persisted rows.</summary>
    public IEnumerable<string>? Labels { get; set; }

    /// <summary>Caller-supplied triples, consumed when <see cref="Infer"/> is <c>triples</c>.</summary>
    public IEnumerable<Triple>? Triples { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.RememberManyAsync"/>.
/// </summary>
public sealed class RememberManyOptions
{
    /// <summary>Existing session to attach the turns to (auto-created when absent).</summary>
    public string? SessionId { get; set; }

    /// <summary>DNF scope selector the batch targets (outer OR, inner AND).</summary>
    public Scope? Scopes { get; set; }

    /// <summary>Bulk extraction strategy (see <see cref="BatchExtractionModes"/>).</summary>
    public string? Extract { get; set; }

    /// <summary>Inference mode (see <see cref="InferModes"/>).</summary>
    public string? Infer { get; set; }

    /// <summary>Descriptive <c>key=value</c> labels for the persisted rows.</summary>
    public IEnumerable<string>? Labels { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.RecallAsync"/>.
/// </summary>
public sealed class RecallOptions
{
    /// <summary>Maximum number of hits to return.</summary>
    public int? K { get; set; }

    /// <summary>Retrieval mode (see <see cref="RecallModes"/>). Defaults to <c>hybrid</c>.</summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Session to record the recall under on its trace. It does not limit the results to that session.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>Result families to include (<c>facts</c>, <c>passages</c>). Defaults to both.</summary>
    public IEnumerable<string>? Include { get; set; }

    /// <summary>Historical query timestamp (known or valid time).</summary>
    public string? AsOf { get; set; }

    /// <summary>System-time query instant (MVCC).</summary>
    public string? AtInstant { get; set; }

    /// <summary><c>key=value</c> label filter the result rows must all carry.</summary>
    public IEnumerable<string>? Labels { get; set; }

    /// <summary>Read lens: a DNF scope selector that narrows the read region (outer OR, inner AND).</summary>
    public Scope? Lens { get; set; }

    /// <summary>Scope read breadth (see <see cref="ScopeViews"/>). Defaults to <c>strict</c>.</summary>
    public string? ScopeView { get; set; }

    /// <summary>Valid-time lower bound.</summary>
    public string? ValidFrom { get; set; }

    /// <summary>Valid-time upper bound.</summary>
    public string? ValidUntil { get; set; }

    /// <summary>Free-form source label recorded on the trace.</summary>
    public string? Source { get; set; }

    /// <summary>Geographic filter applied at read time.</summary>
    public GeoFilterJson? Location { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.ForgetAsync"/>.
/// </summary>
public sealed class ForgetOptions
{
    /// <summary>
    /// Right to be forgotten: also removes the matching supersession history.
    /// Without it, prior rows are retained for audit after the current row is soft-deleted.
    /// </summary>
    public bool? Purge { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.LookupAsync"/>.
/// </summary>
public sealed class LookupOptions
{
    /// <summary>
    /// Skips resolution and answers about this subject directly, as <c>&lt;type&gt;/&lt;name&gt;</c>.
    /// </summary>
    /// <remarks>
    /// This is what makes walking a trail cheap: a hop already knows which entity it landed on,
    /// so re-resolving its name would be both wasted work and a chance to land somewhere else.
    /// </remarks>
    public string? Subject { get; set; }

    /// <summary>Restricts resolution to one entity type.</summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// How far the top candidate must beat the runner-up for the answer to be one entity
    /// rather than a choice between several. Defaults to <c>0.15</c>.
    /// </summary>
    public double? AmbiguityMargin { get; set; }

    /// <summary>
    /// Which sections to fill (see <see cref="LookupSections"/>). Everything but <c>passages</c> by default.
    /// </summary>
    public IEnumerable<string>? Include { get; set; }

    /// <summary>Max facts in the <c>facts</c> section.</summary>
    public int? FactLimit { get; set; }

    /// <summary>Max edges in the <c>relations</c> section.</summary>
    public int? RelationLimit { get; set; }

    /// <summary>Max events in the <c>events</c> section.</summary>
    public int? EventLimit { get; set; }

    /// <summary>Max passages in the <c>passages</c> section.</summary>
    public int? PassageLimit { get; set; }

    /// <summary>Max flags in the <c>uncertainty</c> section.</summary>
    public int? UncertaintyLimit { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.ChatAsync"/> and <see cref="AgentMemoryClient.StreamChatAsync"/>.
/// </summary>
public sealed class ChatOptions
{
    /// <summary>Session to attach the conversation to.</summary>
    public string? SessionId { get; set; }

    /// <summary>DNF scope selector for the conversation (outer OR, inner AND).</summary>
    public Scope? Scopes { get; set; }

    /// <summary>Model override.</summary>
    public string? Model { get; set; }

    /// <summary>Skips the response cache and forces a fresh call.</summary>
    public bool? BypassCache { get; set; }

    /// <summary>Descriptive <c>key=value</c> labels for rows the chat persists.</summary>
    public IEnumerable<string>? Labels { get; set; }

    /// <summary>
    /// Strips inline citation markers from the reply text.
    /// </summary>
    /// <remarks>
    /// With markers left in, the reply carries <c>[S1]</c>-style labels that resolve against <c>citations</c>.
    /// Suppressing them leaves <c>citations</c> intact.
    /// </remarks>
    public bool? SuppressMarkers { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.ContextAsync"/>.
/// </summary>
public sealed class ContextOptions
{
    /// <summary>Maximum number of hits.</summary>
    public int? K { get; set; }

    /// <summary><c>key=value</c> label filter.</summary>
    public IEnumerable<string>? Labels { get; set; }

    /// <summary>Read lens: a DNF scope selector that narrows the read region.</summary>
    public Scope? Lens { get; set; }

    /// <summary>Scope read breadth (see <see cref="ScopeViews"/>).</summary>
    public string? ScopeView { get; set; }

    /// <summary>
    /// Renders one subject's answer rather than the query's hits, as <c>&lt;type&gt;/&lt;name&gt;</c>.
    /// </summary>
    /// <remarks>
    /// This is the copy-as-context export: the fact half of the block becomes this entity's own attributes,
    /// relations and events. The query still selects the passages.
    /// </remarks>
    public string? Subject { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.ReflectAsync"/>.
/// </summary>
public sealed class ReflectOptions
{
    /// <summary>Persists the attributes the reflection derives. Defaults to <see langword="false"/>.</summary>
    public bool Persist { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.ConsolidateAsync"/>.
/// </summary>
public sealed class ConsolidateOptions
{
    /// <summary>Computes the result without writing.</summary>
    public bool? DryRun { get; set; }

    /// <summary>Fact limit for the pass.</summary>
    public int? FactLimit { get; set; }

    /// <summary>Observation limit for the pass.</summary>
    public int? ObservationLimit { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.ElaborateAsync"/>.
/// </summary>
public sealed class ElaborateOptions
{
    /// <summary>The entity to elaborate, by reference. Ignored when <see cref="Sweep"/> is set.</summary>
    public string? EntityRef { get; set; }

    /// <summary>Budget for a sweep. It has no effect on a single-entity pass.</summary>
    public int? Budget { get; set; }

    /// <summary>Runs a sweep over the context, which is also what happens without an <see cref="EntityRef"/>.</summary>
    public bool? Sweep { get; set; }

    /// <summary>Computes the result without writing.</summary>
    public bool? DryRun { get; set; }
}

/// <summary>
/// Options for <see cref="AgentMemoryClient.FsckAsync"/>.
/// </summary>
public sealed class FsckOptions
{
    /// <summary>The check to run.</summary>
    public string? Check { get; set; }

    /// <summary>Similarity threshold for the duplicate check.</summary>
    public double? DuplicateThreshold { get; set; }

    /// <summary>Maximum number of results.</summary>
    public int? MaxResults { get; set; }
}

/// <summary>
/// Filters for <see cref="AgentMemoryClient.AuditAsync"/> and <see cref="AgentMemoryClient.AuditAllAsync"/>.
/// </summary>
public class AuditFilters
{
    /// <summary>Only rows attributed to this principal.</summary>
    public string? Principal { get; set; }

    /// <summary>
    /// Only rows attributed to this API key. The service applies one of the two: it ignores this when
    /// <see cref="Principal"/> is set.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>Only rows of this trace kind.</summary>
    public string? Kind { get; set; }

    /// <summary>
    /// Lower bound on <c>createdAt</c>, as an RFC 3339 timestamp with its offset, for example
    /// <c>2026-01-01T00:00:00Z</c>. The service ignores a value in any other form, and the rows come
    /// back unfiltered.
    /// </summary>
    public string? Since { get; set; }

    /// <summary>
    /// Upper bound on <c>createdAt</c>, as an RFC 3339 timestamp with its offset. The service ignores a
    /// value in any other form.
    /// </summary>
    public string? Until { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="AgentMemoryClient.AuditAsync"/>.
/// <c>/audit</c> takes <c>limit</c> and <c>cursor</c> only, so there is no count to offer.
/// </summary>
public sealed class AuditOptions : AuditFilters
{
    /// <summary>Page size.</summary>
    public int? Limit { get; set; }

    /// <summary>Continuation token from the previous page.</summary>
    public string? Cursor { get; set; }
}
