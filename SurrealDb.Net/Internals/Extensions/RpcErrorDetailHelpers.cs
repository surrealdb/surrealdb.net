using System.Collections.Generic;

namespace SurrealDb.Net.Internals.Extensions;

internal static class RpcErrorDetailHelpers
{
    /// <summary>Gets the "kind" string from a detail object.</summary>
    internal static string? DetailKind(IReadOnlyDictionary<string, object?>? details)
    {
        if (details == null || !details.TryGetValue("kind", out var k) || k is not string s)
            return null;
        return s;
    }

    /// <summary>Gets the inner "details" map when present.</summary>
    internal static IReadOnlyDictionary<string, object?>? DetailInner(
        IReadOnlyDictionary<string, object?>? details
    )
    {
        if (details == null || !details.TryGetValue("details", out var d))
            return null;
        return d as IReadOnlyDictionary<string, object?>;
    }

    /// <summary>
    /// Gets a string field, supporting both nested { kind, details: { field } } and flat { field }.
    /// </summary>
    internal static string? DetailField(
        IReadOnlyDictionary<string, object?>? details,
        string kind,
        string field
    )
    {
        if (details == null)
            return null;
        var inner = DetailInner(details);
        if (
            DetailKind(details) == kind
            && inner != null
            && inner.TryGetValue(field, out var v)
            && v is string vs
        )
            return vs;
        if (details.TryGetValue(field, out var v2) && v2 is string vs2)
            return vs2;
        return null;
    }

    /// <summary>Gets the "kind" of a nested detail (e.g. "TokenExpired" from "Auth").</summary>
    internal static string? DetailInnerKind(IReadOnlyDictionary<string, object?>? details)
    {
        return DetailKind(DetailInner(details));
    }

    /// <summary>Parses duration.secs and duration.nanos for query timeout errors.</summary>
    internal static (int seconds, int nanos)? DetailTimeoutDuration(
        IReadOnlyDictionary<string, object?>? details
    )
    {
        if (
            details == null
            || !details.TryGetValue("duration", out var d)
            || d is not IReadOnlyDictionary<string, object?> duration
        )
            return null;
        if (
            !duration.TryGetValue("secs", out var secsObj)
            || !duration.TryGetValue("nanos", out var nanosObj)
        )
            return null;
        var secs = secsObj switch
        {
            long l => (int)l,
            int i => i,
            _ => 0,
        };
        var nanos = nanosObj switch
        {
            long l => (int)l,
            int i => i,
            _ => 0,
        };
        return (secs, nanos);
    }
}
