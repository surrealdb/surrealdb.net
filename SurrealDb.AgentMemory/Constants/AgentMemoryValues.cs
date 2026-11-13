namespace SurrealDb.AgentMemory;

/// <summary>
/// Inference modes for <see cref="AgentMemoryClient.RememberAsync"/>.
/// The mode decides which extraction path runs and which provenance kind the writes are tagged with.
/// </summary>
public static class InferModes
{
    /// <summary><c>full</c>, the default.</summary>
    public const string Full = "full";

    /// <summary><c>triples</c>: consumes the caller-supplied triples.</summary>
    public const string Triples = "triples";

    /// <summary><c>preview</c>.</summary>
    public const string Preview = "preview";

    /// <summary><c>none</c>.</summary>
    public const string None = "none";
}

/// <summary>
/// Bulk extraction strategies for <see cref="AgentMemoryClient.RememberManyAsync"/>.
/// </summary>
public static class BatchExtractionModes
{
    /// <summary><c>per_message</c>: one extraction per message.</summary>
    public const string PerMessage = "per_message";

    /// <summary><c>whole_conversation</c>: one extraction over the transcript.</summary>
    public const string WholeConversation = "whole_conversation";
}

/// <summary>
/// Memory categories applied during extraction.
/// </summary>
public static class MemoryCategories
{
    /// <summary><c>identity</c>.</summary>
    public const string Identity = "identity";

    /// <summary><c>knowledge</c>.</summary>
    public const string Knowledge = "knowledge";

    /// <summary><c>context</c>: swept by <see cref="Lifecycle.ExpireAsync"/>.</summary>
    public const string Context = "context";
}

/// <summary>
/// Roles of a conversation turn participant.
/// </summary>
public static class TurnRoles
{
    /// <summary><c>user</c>, the default for <see cref="AgentMemoryClient.RememberAsync"/>.</summary>
    public const string User = "user";

    /// <summary><c>assistant</c>.</summary>
    public const string Assistant = "assistant";

    /// <summary><c>system</c>.</summary>
    public const string System = "system";

    /// <summary><c>tool</c>.</summary>
    public const string Tool = "tool";
}

/// <summary>
/// Retrieval modes for <see cref="Documents.QueryAsync"/>:
/// vector only, BM25 only, hybrid RRF, or hybrid plus a graph-density re-rank.
/// </summary>
/// <remarks>
/// A memory recall has its own modes: see <see cref="RecallModes"/>.
/// </remarks>
public static class QueryModes
{
    /// <summary><c>hybrid</c>: vector and BM25, fused with RRF.</summary>
    public const string Hybrid = "hybrid";

    /// <summary><c>vector</c>: vector similarity only.</summary>
    public const string Vector = "vector";

    /// <summary><c>bm25</c>: BM25 only.</summary>
    public const string Bm25 = "bm25";

    /// <summary><c>hybrid_graph</c>: hybrid plus a graph-density re-rank.</summary>
    public const string HybridGraph = "hybrid_graph";
}

/// <summary>
/// Retrieval modes for <see cref="AgentMemoryClient.RecallAsync"/>.
/// </summary>
/// <remarks>
/// These are not the modes of a document query (<see cref="QueryModes"/>): a recall has <c>graph</c>,
/// and refuses <c>hybrid_graph</c>.
/// </remarks>
public static class RecallModes
{
    /// <summary><c>hybrid</c>, the default.</summary>
    public const string Hybrid = "hybrid";

    /// <summary><c>vector</c>.</summary>
    public const string Vector = "vector";

    /// <summary><c>bm25</c>.</summary>
    public const string Bm25 = "bm25";

    /// <summary><c>graph</c>.</summary>
    public const string Graph = "graph";
}

/// <summary>
/// Grant verbs in the scope permission model, in the <c>noun:verb</c> form the service reads and returns.
/// </summary>
public static class Verbs
{
    /// <summary><c>memory:read</c>: read what is stored in a scope.</summary>
    public const string MemoryRead = "memory:read";

    /// <summary><c>memory:write</c>: write into a scope.</summary>
    public const string MemoryWrite = "memory:write";

    /// <summary><c>memory:forget</c>: erase what is stored in a scope.</summary>
    public const string MemoryForget = "memory:forget";

    /// <summary><c>scope:read</c>: see the scope tree.</summary>
    public const string ScopeRead = "scope:read";

    /// <summary><c>scope:create</c>: register scopes.</summary>
    public const string ScopeCreate = "scope:create";

    /// <summary><c>scope:delete</c>: delete scopes.</summary>
    public const string ScopeDelete = "scope:delete";

    /// <summary><c>grant:manage</c>: administer principals and their grants.</summary>
    public const string GrantManage = "grant:manage";
}

/// <summary>
/// Scope read breadth for memory queries.
/// Only <see cref="Strict"/> is enforced; the others are accepted on the wire and behave as <see cref="Strict"/>.
/// </summary>
public static class ScopeViews
{
    /// <summary><c>strict</c>, the default.</summary>
    public const string Strict = "strict";

    /// <summary><c>merged</c>.</summary>
    public const string Merged = "merged";

    /// <summary><c>crossTeam</c>.</summary>
    public const string CrossTeam = "crossTeam";
}

/// <summary>
/// Orderings for the ranked entity head (<see cref="Entities.TopAsync"/>).
/// </summary>
/// <remarks>
/// <see cref="Coverage"/> is most-known-about and the one ordering the entity listing cannot express,
/// but it costs an exact aggregate pass per fact family. <see cref="Importance"/> and <see cref="Recency"/>
/// are index-served single-table reads: prefer them where the ranking need not be exact.
/// </remarks>
public static class EntityRankings
{
    /// <summary><c>coverage</c>, the default.</summary>
    public const string Coverage = "coverage";

    /// <summary><c>importance</c>.</summary>
    public const string Importance = "importance";

    /// <summary><c>recency</c>.</summary>
    public const string Recency = "recency";
}

/// <summary>
/// The sections <see cref="AgentMemoryClient.LookupAsync"/> can be asked to fill.
/// </summary>
/// <remarks>
/// Everything but <see cref="Passages"/> is on by default, because passages cost a retrieval pass.
/// An omitted section comes back empty with <c>truncated</c> false: it was declined, not cut short.
/// Unknown names are ignored by the server. <c>entities</c> is not selectable: the server fills it for a topic answer.
/// </remarks>
public static class LookupSections
{
    /// <summary><c>facts</c>.</summary>
    public const string Facts = "facts";

    /// <summary><c>relations</c>.</summary>
    public const string Relations = "relations";

    /// <summary><c>events</c>.</summary>
    public const string Events = "events";

    /// <summary><c>passages</c>.</summary>
    public const string Passages = "passages";

    /// <summary><c>uncertainty</c>.</summary>
    public const string Uncertainty = "uncertainty";
}

/// <summary>
/// Document pipeline statuses.
/// </summary>
public static class DocumentStatuses
{
    /// <summary><c>queued</c>.</summary>
    public const string Queued = "queued";

    /// <summary><c>extracting</c>.</summary>
    public const string Extracting = "extracting";

    /// <summary><c>chunking</c>.</summary>
    public const string Chunking = "chunking";

    /// <summary><c>embedding</c>.</summary>
    public const string Embedding = "embedding";

    /// <summary><c>keywording</c>.</summary>
    public const string Keywording = "keywording";

    /// <summary><c>extracting_nodes</c>.</summary>
    public const string ExtractingNodes = "extracting_nodes";

    /// <summary><c>ready</c>.</summary>
    public const string Ready = "ready";

    /// <summary><c>failed</c>.</summary>
    public const string Failed = "failed";
}
