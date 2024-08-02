#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Text;

namespace SurrealDb.Net.Handlers;

/// <summary>
/// A custom-tailored <see cref="InterpolatedStringHandlerAttribute"/> to interpret query strings
/// passed down to <see cref="SurrealDbClient.Query(QueryInterpolatedStringHandler, CancellationToken)"/>.
/// </summary>
[InterpolatedStringHandler]
public ref struct QueryInterpolatedStringHandler
{
    private string? _literalQuery;
    private readonly StringBuilder? _builder;
    private readonly Dictionary<string, object?> _parameters = [];

    public QueryInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        if (formattedCount > 0)
        {
            _builder = new(literalLength + CalculateParameterNamesExpectedLength(formattedCount));
        }
    }

    private static int CalculateParameterNamesExpectedLength(int formattedCount)
    {
        const int parameterNameExpectedLength = 3;
        return parameterNameExpectedLength * formattedCount;
    }

    public void AppendLiteral(string s)
    {
        if (_builder is null)
        {
            _literalQuery = s;
            return;
        }

        _builder.Append(s);
    }

    public void AppendFormatted<T>(T t)
    {
        if (_builder is null)
        {
            return;
        }

        string? parameterName = null;

        foreach (var parameter in _parameters)
        {
            if (t?.Equals(parameter.Value) == true)
            {
                parameterName = parameter.Key;
                break;
            }
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            parameterName = $"p{_parameters.Count}";
            _parameters.Add(parameterName, t);
        }

        _builder.Append('$');
        _builder.Append(parameterName);
    }

    public string GetFormattedText() =>
        _builder is null ? _literalQuery ?? string.Empty : _builder.ToString();

    public IReadOnlyDictionary<string, object?> GetParameters() => _parameters;
}
#endif
