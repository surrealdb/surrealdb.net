using SurrealDb.Internals.Models;
using System.Text.Json.Serialization;

namespace SurrealDb.Models;

public partial class Thing
{
	private readonly ReadOnlyMemory<char> _raw;
	private readonly int _separatorIndex;
	private readonly bool _isEscaped;
	private readonly SpecialRecordIdType _specialRecordIdType = SpecialRecordIdType.None;

	private int _startIdIndex => _separatorIndex + 1;
	private int _endIdIndex => _raw.Length - 1;

	internal ReadOnlySpan<char> UnescapedIdSpan
	{
		get
		{
			if (_isEscaped)
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
