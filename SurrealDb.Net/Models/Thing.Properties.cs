using SurrealDb.Net.Internals.Models;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Models;

public partial class Thing
{
    private readonly ReadOnlyMemory<char> _raw;
    private readonly int _separatorIndex;
    private readonly bool _isTableEscaped;
    private readonly bool _isIdEscaped;
    private readonly SpecialRecordPartType _specialTableType = SpecialRecordPartType.None;
    private readonly SpecialRecordPartType _specialRecordIdType = SpecialRecordPartType.None;

    private int _startTableIndex => 0;
    private int _endTableIndex => _separatorIndex - 1;

    internal ReadOnlySpan<char> UnescapedTableSpan
    {
        get
        {
            if (_isTableEscaped)
                return _raw.Span[(_startTableIndex + 1).._endTableIndex];

            return TableSpan;
        }
    }
    internal string UnescapedTable => UnescapedTableSpan.ToString();

    private int _startIdIndex => _separatorIndex + 1;
    private int _endIdIndex => _raw.Length - 1;

    internal ReadOnlySpan<char> UnescapedIdSpan
    {
        get
        {
            if (_isIdEscaped)
                return _raw.Span[(_startIdIndex + 1).._endIdIndex];

            return IdSpan;
        }
    }
    internal string UnescapedId => UnescapedIdSpan.ToString();

    [JsonIgnore]
    public ReadOnlySpan<char> TableSpan => _raw.Span[.._separatorIndex];

    [JsonIgnore]
    public ReadOnlySpan<char> IdSpan => _raw.Span[_startIdIndex..];

    public string Table => TableSpan.ToString();
    public string Id => IdSpan.ToString();
}
