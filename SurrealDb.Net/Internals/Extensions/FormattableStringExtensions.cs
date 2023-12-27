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

        var parameters = new Dictionary<string, object?>(capacity: query.ArgumentCount);

        var arguments = query.GetArguments();
        int index = 0;

        foreach (var argument in arguments)
        {
            string parameterName = $"p{index}";
            parameters.Add(parameterName, argument);

            index++;
        }

        string formattedQuery = string.Format(
            CultureInfo.InvariantCulture,
            query.Format,
            parameters.Select(p => $"${p.Key}").ToArray()
        );

        return (formattedQuery, parameters);
    }
}
