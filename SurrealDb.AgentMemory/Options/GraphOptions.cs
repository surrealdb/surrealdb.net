namespace SurrealDb.AgentMemory;

/// <summary>
/// Filters and pagination for <see cref="Entities.ListAsync"/>.
/// </summary>
public sealed class EntityListOptions : PageOptions
{
    /// <summary>Only entities of this type.</summary>
    public string? Type { get; set; }
}

/// <summary>
/// Options for <see cref="Entities.GetAsync"/>.
/// </summary>
public sealed class EntityGetOptions : TemporalOptions
{
    /// <summary>
    /// Max rows per fact section (default 500, which is also the cap).
    /// </summary>
    /// <remarks>
    /// Both sections are a bounded head, not the whole set: check <c>truncated.attributes</c> and
    /// <c>truncated.relations</c>, and walk the section's own collection when either is set.
    /// </remarks>
    public int? Limit { get; set; }
}

/// <summary>
/// Options for <see cref="Entities.SearchAsync"/>.
/// </summary>
public sealed class EntitySearchOptions
{
    /// <summary>Restricts to one entity type.</summary>
    public string? Type { get; set; }

    /// <summary>Max matches (default 10, capped by the server's list limit).</summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Options for <see cref="Entities.TopAsync"/>.
/// </summary>
public sealed class TopEntitiesOptions
{
    /// <summary>Ordering (see <see cref="EntityRankings"/>). Defaults to <c>coverage</c>.</summary>
    public string? By { get; set; }

    /// <summary>Restricts to one entity type.</summary>
    public string? Type { get; set; }

    /// <summary>Max entities (default 10).</summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Options for <see cref="Entities.NeighboursAsync"/>.
/// </summary>
/// <remarks>
/// A total (<see cref="PageOptions.Count"/> set to <see langword="true"/>) cannot be asked for together
/// with a <see cref="MinFacts"/> above zero: the server rejects the pairing with a <c>400</c>, because
/// honouring the filter in a total would cost the per-neighbour counts the walk exists to avoid. The
/// client refuses it before sending.
/// </remarks>
public sealed class NeighbourhoodOptions : PageOptions
{
    /// <summary>Only neighbours with at least this many facts.</summary>
    public int? MinFacts { get; set; }

    internal void Validate()
    {
        if (MinFacts > 0 && Count == true)
        {
            throw new ArgumentException(
                "A count cannot be combined with MinFacts above zero: the server rejects that pairing."
            );
        }
    }
}

/// <summary>
/// Filters <c>/attributes</c> accepts, beside its pagination parameters.
/// </summary>
public class AttributeFilters
{
    /// <summary>One entity's attributes, as <c>&lt;type&gt;/&lt;name&gt;</c> (for example <c>person/alice</c>).</summary>
    public string? Entity { get; set; }

    /// <summary>One attribute key.</summary>
    public string? Key { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="Facts.AttributesAsync"/>.
/// </summary>
public sealed class AttributeListOptions : PageOptions
{
    /// <summary>One entity's attributes, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Entity { get; set; }

    /// <summary>One attribute key.</summary>
    public string? Key { get; set; }
}

/// <summary>
/// Filters <c>/relations</c> accepts, beside its pagination parameters.
/// </summary>
public class RelationFilters
{
    /// <summary>Subject entity, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Src { get; set; }

    /// <summary>Object entity, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Dst { get; set; }

    /// <summary>Relation label.</summary>
    public string? Label { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="Facts.RelationsAsync"/>.
/// </summary>
public sealed class RelationListOptions : PageOptions
{
    /// <summary>Subject entity, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Src { get; set; }

    /// <summary>Object entity, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Dst { get; set; }

    /// <summary>Relation label.</summary>
    public string? Label { get; set; }
}

/// <summary>
/// Filters <c>/actions</c> accepts, beside its pagination parameters.
/// </summary>
public class ActionFilters
{
    /// <summary>Acting entity, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Actor { get; set; }

    /// <summary>Action verb.</summary>
    public string? Verb { get; set; }

    /// <summary>Inclusive lower bound on <c>occurredAt</c>, as an RFC 3339 timestamp.</summary>
    public string? Since { get; set; }

    /// <summary>Inclusive upper bound on <c>occurredAt</c>, as an RFC 3339 timestamp.</summary>
    public string? Until { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="Facts.ActionsAsync"/>.
/// </summary>
public sealed class ActionListOptions : PageOptions
{
    /// <summary>Acting entity, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Actor { get; set; }

    /// <summary>Action verb.</summary>
    public string? Verb { get; set; }

    /// <summary>Inclusive lower bound on <c>occurredAt</c>, as an RFC 3339 timestamp.</summary>
    public string? Since { get; set; }

    /// <summary>Inclusive upper bound on <c>occurredAt</c>, as an RFC 3339 timestamp.</summary>
    public string? Until { get; set; }
}

/// <summary>
/// Filters for <see cref="Uncertainty.ListAllAsync"/> and <see cref="Uncertainty.CountAsync"/>.
/// </summary>
public class UncertaintyFilters
{
    /// <summary>Restricts to one subject, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Entity { get; set; }

    /// <summary>
    /// Filters on settled state. Unset returns both, so pass <see langword="false"/> for the open flags.
    /// </summary>
    public bool? Resolved { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="Uncertainty.ListAsync"/>.
/// </summary>
public sealed class UncertaintyListOptions : PageOptions
{
    /// <summary>Restricts to one subject, as <c>&lt;type&gt;/&lt;name&gt;</c>.</summary>
    public string? Entity { get; set; }

    /// <summary>
    /// Filters on settled state. Unset returns both, so pass <see langword="false"/> for the open flags.
    /// </summary>
    public bool? Resolved { get; set; }
}

/// <summary>
/// Options for <see cref="Uncertainty.ResolveAsync"/>.
/// </summary>
public sealed class ResolveUncertaintyOptions
{
    /// <summary>
    /// Why this value was chosen, persisted as the new row's source clause, so the fact reads as an operator
    /// decision rather than an unexplained high-trust assertion. Stored normalized and capped.
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Options for <see cref="Keys.CreateAsync"/>.
/// </summary>
public sealed class KeyCreateOptions
{
    /// <summary>A name for the key.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Grants that narrow the key: scope patterns per verb, for example
    /// <c>{ "memory:read": ["team/eng/*"] }</c> (see <see cref="Verbs"/>). Each pattern must lie within
    /// what the caller holds for that verb. Unset means the key inherits the principal's grants.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>>? Grants { get; set; }

    /// <summary>Lifetime of the key, in seconds.</summary>
    public int? TtlSeconds { get; set; }
}

/// <summary>
/// Options for <see cref="Sessions.CreateAsync"/>.
/// </summary>
public sealed class SessionCreateOptions
{
    /// <summary>DNF scope selector the session writes to (outer OR, inner AND).</summary>
    public Scope? Scopes { get; set; }

    /// <summary>Free-form metadata stored with the session.</summary>
    public System.Text.Json.Nodes.JsonNode? Metadata { get; set; }
}
