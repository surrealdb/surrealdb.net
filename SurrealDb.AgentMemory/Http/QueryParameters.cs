using System.Collections;
using System.Globalization;

namespace SurrealDb.AgentMemory;

/// <summary>
/// An ordered set of query-string parameters.
/// A parameter is added only when its value is present, so absent options stay absent.
/// </summary>
internal sealed class QueryParameters : IEnumerable<KeyValuePair<string, string?>>
{
    private readonly List<KeyValuePair<string, string?>> _values = [];

    public QueryParameters Add(string key, string? value)
    {
        if (value is not null)
        {
            Set(key, value);
        }
        return this;
    }

    public QueryParameters Add(string key, bool? value)
    {
        if (value is not null)
        {
            Set(key, value.Value ? "true" : "false");
        }
        return this;
    }

    public QueryParameters Add(string key, int? value)
    {
        if (value is not null)
        {
            Set(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
        return this;
    }

    public QueryParameters Add(string key, long? value)
    {
        if (value is not null)
        {
            Set(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
        return this;
    }

    public QueryParameters Add(string key, double? value)
    {
        if (value is not null)
        {
            Set(key, value.Value.ToString("R", CultureInfo.InvariantCulture));
        }
        return this;
    }

    /// <summary>
    /// Copies the cursor-pagination parameters a caller supplied.
    /// </summary>
    public QueryParameters AddCursor(CursorOptions? options)
    {
        if (options is not null)
        {
            Add("limit", options.Limit);
            Add("cursor", options.Cursor);
        }
        return this;
    }

    /// <summary>
    /// Copies the pagination parameters a caller supplied, leaving absent ones absent
    /// so the server applies its own defaults.
    /// </summary>
    public QueryParameters AddPage(PageOptions? options)
    {
        if (options is not null)
        {
            AddCursor(options);
            Add("count", options.Count);
        }
        return this;
    }

    /// <summary>
    /// Copies the deprecated offset parameters, kept apart from <see cref="AddPage"/> because the server
    /// rejects a cursor sent together with a page index.
    /// </summary>
    public QueryParameters AddOffset(CursorOrOffsetOptions? options)
    {
        if (options is not null)
        {
            Add("page", options.Page);
            Add("pageSize", options.PageSize);
        }
        return this;
    }

    public QueryParameters AddTemporal(TemporalOptions? options)
    {
        if (options is not null)
        {
            Add("asOf", options.AsOf);
            Add("atInstant", options.AtInstant);
            Add("validFrom", options.ValidFrom);
            Add("validUntil", options.ValidUntil);
        }
        return this;
    }

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void Set(string key, string value)
    {
        var index = _values.FindIndex(pair => pair.Key == key);
        if (index >= 0)
        {
            _values[index] = new(key, value);
        }
        else
        {
            _values.Add(new(key, value));
        }
    }
}
