using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Models;

public readonly partial struct Duration
{
    /// <summary>
    /// Creates a default Duration (0ns)
    /// </summary>
    public Duration()
    {
        _value = DurationConstants.DefaultDuration;
        _unitValues = new();
    }

    internal Duration(string value)
    {
        _value = value;
        _unitValues = DurationParser
            .Parse(value)
            .Where(kv => kv.value != 0)
            .ToDictionary(kv => kv.unit, kv => (int)kv.value);
    }

    internal Duration(long? seconds, int? nanos)
    {
        _unitValues = DurationParser.Convert(seconds, nanos);
        _value = ""; // TODO
    }
}
