using System.Collections.Immutable;
using System.Globalization;

namespace SurrealDb.Net.Internals.Extensions;

internal static class FormattableStringExtensions
{
    public static (string, IReadOnlyDictionary<string, object?>) ExtractRawQueryParams(
        this FormattableString query
    )
    {
        if (query.ArgumentCount == 0)
        {
            return (query.Format, ImmutableDictionary<string, object?>.Empty);
        }

        var formatArgs = new string[query.ArgumentCount];
        var parameters = new Dictionary<string, object?>();

        var arguments = query.GetArguments();
        int currentParameterIndex = 0;

        for (int index = 0; index < arguments.Length; index++)
        {
            ReadOnlySpan<char> parameterName = default;

            foreach (var parameter in parameters)
            {
                if (parameter.Value == arguments[index])
                {
                    parameterName = parameter.Key;
                    break;
                }
            }

            if (parameterName.IsEmpty)
            {
                parameterName = $"p{currentParameterIndex++}";
                parameters.Add(parameterName.ToString(), arguments[index]);
            }

#if NET6_0_OR_GREATER
            formatArgs[index] = $"${parameterName}";
#else
            formatArgs[index] = $"${parameterName.ToString()}";
#endif
        }

        string formattedQuery = string.Format(
            CultureInfo.InvariantCulture,
            query.Format,
            formatArgs
        );

        return (formattedQuery, parameters);
    }
}
