namespace SurrealDb.AgentMemory.Tests.Infrastructure;

/// <summary>
/// Minimal response bodies that satisfy the generated models' required fields.
/// </summary>
public static class Fixtures
{
    /// <summary>
    /// A timestamp as the service writes it: nanosecond precision, so nine fractional digits.
    /// </summary>
    public const string Timestamp = "2026-01-01T00:00:00.123456789Z";

    public static string Page(
        bool hasMore = false,
        string? nextCursor = null,
        long? totalSize = null
    )
    {
        var cursor = nextCursor is null ? "" : $",\"nextCursor\":\"{nextCursor}\"";
        var total = totalSize is null ? "" : $",\"totalSize\":{totalSize}";
        return $"{{\"hasMore\":{(hasMore ? "true" : "false")}{cursor}{total}}}";
    }

    public static string EntityDetail(string name, string type = "person")
    {
        return $$"""{"id":"entity:['{{type}}','{{name}}']","name":"{{name}}","entityType":"{{type}}","memoryCategory":"identity","importance":0.5,"createdAt":"{{Timestamp}}","updatedAt":"{{Timestamp}}"}""";
    }

    public static string EntityMatch(string name, string type = "person")
    {
        return $$"""{"entity":{{EntityDetail(name, type)}},"score":1,"factCount":4}""";
    }

    public static string Attribute(string id)
    {
        return $$"""{"id":"{{id}}","entity":"entity:1","key":"employer","value":"SurrealDB","memoryCategory":"identity","importance":0.5,"confidence":0.9,"scope":[],"labels":[],"createdAt":"{{Timestamp}}"}""";
    }

    public static string Relation(string id)
    {
        return $$"""{"id":"{{id}}","subject":"person/tobie","object":"company/acme","label":"works_at","memoryCategory":"knowledge","confidence":0.9,"scope":[],"labels":[],"createdAt":"{{Timestamp}}"}""";
    }

    public static string Action(string id)
    {
        return $$"""{"id":"{{id}}","actor":"person/tobie","verb":"shipped","summary":"Shipped the SDK","memoryCategory":"knowledge","confidence":0.9,"createdAt":"{{Timestamp}}"}""";
    }

    public static string Uncertainty(string id, bool resolved = false, bool resolvable = true)
    {
        return $$"""{"id":"{{id}}","about":"tobie's employer","reason":"contradiction","resolved":{{(resolved ? "true" : "false")}},"resolvable":{{(resolvable ? "true" : "false")}},"scope":[],"labels":[],"createdAt":"{{Timestamp}}","entity":"entity:person/tobie","key":"employer"}""";
    }

    public static string Neighbour(string label, bool outbound, string farName, string relationId)
    {
        return $$"""{"label":"{{label}}","outbound":{{(outbound ? "true" : "false")}},"far":{{EntityMatch(farName, "company")}},"relationId":"{{relationId}}"}""";
    }

    public const string ExtractionResult =
        """{"turnId":"turn:1","entities":[],"attributes":[],"relations":[],"actions":[],"instructions":[],"uncertainties":[],"corrections":[]}""";

    public const string Citation =
        """{"marker":"S1","id":"chunk:1","kind":"passage","snippet":"Tobie is the CTO.","score":0.9}""";

    public const string FactsResponse = """{"mode":"full","sessionId":"s1"}""";

    public static readonly string FactsBatchResponse =
        $$"""{"sessionId":"s1","turnIds":["turn:1"],"extractions":[{{ExtractionResult}}]}""";

    public const string QueryMemoryResponse =
        """{"tier":"direct","hits":[],"queryMs":0,"trace":{"traceId":"trace:1","resolutionTier":"direct","tierReason":"exact","latencyMs":1,"retrievedCount":0,"topScores":[]},"classificationKind":"hybrid","seedEntities":[]}""";

    public const string ContextResponse = """{"context":"blob","tier":"direct","queryMs":1}""";

    public static readonly string ChatResponse =
        $$"""{"reply":"You are the CTO.","memoryUpdates":{{ExtractionResult}},"traceId":"trace:1","sessionId":"s1","citations":[{{Citation}}]}""";

    public const string SessionResponse =
        """{"id":"s1","scopes":[["u/1"]],"createdAt":"2026-01-01"}""";

    public const string Whoami =
        """{"principalId":"principal:tobie","kind":"user","displayName":"Tobie","grants":{},"effectiveGrants":{}}""";

    public static string MintedKey(string id, string key)
    {
        return $$"""{"id":"{{id}}","key":"{{key}}"}""";
    }

    public static string KeyDetail(string id)
    {
        return $$"""{"id":"{{id}}","name":"{{id}}","createdAt":"{{Timestamp}}"}""";
    }

    public static string Upload(string id)
    {
        return $$"""{"id":"{{id}}","status":"queued","contentHash":"x","deduplicated":false}""";
    }

    public const string QueryResponse = """{"results":[],"queryMs":1}""";

    public const string ForgetResponse = """{"deleted":3}""";

    public const string ReflectResponse =
        """{"reflection":"Nothing changed.","evidence":[],"persistedAttributes":[],"traceId":"trace:1"}""";

    public const string ConsolidateResponse =
        """{"traceId":"trace:1","created":1,"updated":0,"superseded":0,"dryRun":true,"outcomes":[]}""";

    public const string ElaborateResponse = """{"relationsEmitted":1,"outcomes":[]}""";

    public const string FsckResponse =
        """{"duplicates":[],"contradictions":[],"injection":[],"unscopedContent":[],"total":0}""";

    public static string AuditRow(string traceId)
    {
        return $$"""{"traceId":"{{traceId}}","kind":"retrieval","cost":0,"rowsTouched":1,"latencyMs":1,"createdAt":"{{Timestamp}}"}""";
    }

    private const string CategoryState =
        """{"entities":[],"attributes":[],"relations":[],"actions":[]}""";

    public static readonly string StateResponse =
        $$$"""{"identity":{{{CategoryState}}},"knowledge":{{{CategoryState}}},"context":{{{CategoryState}}},"instructions":[],"unknowns":[],"truncated":{"entities":false,"attributes":false,"relations":false,"actions":false,"instructions":true,"unknowns":false}}""";

    public const string ProfileResponse =
        """{"selfFacts":[],"static":[],"dynamic":[],"preferences":[],"instructions":[]}""";

    public static string Document(string id)
    {
        return $$"""{"id":"{{id}}","title":"Handbook","mimeType":"text/plain","source":"https://example.test/a.txt","contentHash":"x","sizeBytes":3,"version":1,"status":"ready","createdAt":"{{Timestamp}}","updatedAt":"{{Timestamp}}"}""";
    }

    public static string Chunk(string id)
    {
        return $$"""{"id":"{{id}}","document":"document:1","position":0,"charStart":0,"charEnd":3,"text":"abc"}""";
    }

    public static string TraceRecord(string id)
    {
        return $$"""{"id":"{{id}}","queryText":"q","resolutionTier":"direct","tierReason":"exact","latencyMs":1,"cached":false,"createdAt":"{{Timestamp}}"}""";
    }

    public static string ScopeNode(string path)
    {
        return $$"""{"path":"{{path}}","createdAt":"{{Timestamp}}"}""";
    }

    public static string Principal(string id)
    {
        return $$$"""{"id":"{{{id}}}","kind":"user","displayName":"Alex","grants":{"team/eng":["read"]}}""";
    }

    public static string Turn(string id)
    {
        return $$"""{"id":"{{id}}","session":"s1","role":"user","content":"hi","seq":0,"createdAt":"{{Timestamp}}"}""";
    }

    public static string Keyword(string id)
    {
        return $$"""{"id":"{{id}}","text":"Needle","normalised":"needle","documentCount":1}""";
    }

    public static string DocumentKeyword(string id)
    {
        return $$"""{"id":"{{id}}","text":"Needle","normalised":"needle","score":0.5}""";
    }

    private const string EmptySection = """{"items":[],"truncated":false}""";

    public static string Lookup(string resolution)
    {
        return $$"""{"resolution":{{resolution}},"facts":{{EmptySection}},"relations":{{EmptySection}},"events":{{EmptySection}},"passages":{{EmptySection}},"entities":{{EmptySection}},"uncertainty":{{EmptySection}}}""";
    }

    public static string EntityResponse(bool attributesTruncated)
    {
        return $$$"""{"entity":{{{EntityDetail("tobie")}}},"attributes":[],"relations":[],"truncated":{"attributes":{{{(attributesTruncated ? "true" : "false")}}},"relations":false}}""";
    }
}
