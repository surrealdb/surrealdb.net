using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Extensions;

internal static partial class SurrealDbLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Attempting to connect to '{endpoint}'."
    )]
    public static partial void LogConnectionAttempt(this ILogger logger, string endpoint);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Connection set to namespace='{namespace}'."
    )]
    public static partial void LogConnectionNamespaceSet(this ILogger logger, string? @namespace);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Connection set to namespace='{namespace}', database='{database}'."
    )]
    public static partial void LogConnectionNamespaceAndDatabaseSet(
        this ILogger logger,
        string? @namespace,
        string database
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Connection signed as root, user={username}, password={password}."
    )]
    public static partial void LogConnectionSignedAsRoot(
        this ILogger logger,
        string username,
        string password
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Connection signed via token, token={token}."
    )]
    public static partial void LogConnectionSignedViaJwt(this ILogger logger, string token);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully connected to '{endpoint}'."
    )]
    public static partial void LogConnectionSuccess(this ILogger logger, string endpoint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Method '{method}' failed. Reason: {reason}")]
    public static partial void LogMethodFailed(this ILogger logger, string method, string reason);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Method '{method}' executed successfully.\nParameters: {parameters}\nExecution time: {executionTime}"
    )]
    public static partial void LogMethodSuccess(
        this ILogger logger,
        string method,
        string parameters,
        string executionTime
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Request #{requestId} failed. Reason: {reason}"
    )]
    public static partial void LogRequestFailed(
        this ILogger logger,
        string requestId,
        string reason
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Request #{requestId} executed successfully.\nMethod: '{method}'\nParameters: {parameters}\nExecution time: {executionTime}"
    )]
    public static partial void LogRequestSuccess(
        this ILogger logger,
        string requestId,
        string method,
        string parameters,
        string executionTime
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Query executed successfully.\n{query}\nParameters: {parameters}\nExecution time: {executionTime}"
    )]
    public static partial void LogQuerySuccess(
        this ILogger logger,
        string query,
        string parameters,
        string executionTime
    );

    public static string FormatRequestParameters(
        object?[]? parameters,
        bool shouldLogParameterValues
    )
    {
        if (parameters is null || parameters.Length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        builder.Append('[');

        for (int index = 0; index < parameters.Length; index++)
        {
            var value = parameters[index];

            FormatParameterValue(builder, value, shouldLogParameterValues);

            if (index == parameters.Length - 1)
            {
                break;
            }

            builder.Append(", ");
        }

        builder.Append(']');

        return builder.ToString();
    }

    public static string FormatQueryParameters(
        IReadOnlyDictionary<string, object?> parameters,
        bool shouldLogParameterValues
    )
    {
        if (parameters.Count <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        int index = 0;

        foreach (var (name, value) in parameters)
        {
            builder.Append('$').Append(name);
            builder.Append('=');
            FormatParameterValue(builder, value, shouldLogParameterValues);

            index++;

            if (index >= parameters.Count)
            {
                break;
            }

            builder.Append(", ");
        }

        return builder.ToString();
    }

    private static void FormatParameterValue(
        StringBuilder builder,
        object? parameterValue,
        bool shouldLogParameterValue
    )
    {
        if (!shouldLogParameterValue)
        {
            builder.Append(LoggingConstants.SENSITIVE_VALUE_PLACEHOLDER);
            return;
        }

        if (parameterValue is null)
        {
            builder.Append("null");
            return;
        }

        builder.Append('\'');

        switch (parameterValue)
        {
            case DateTime dateTimeValue:
                builder.Append(dateTimeValue.ToString("s"));
                break;
            case DateTimeOffset dateTimeOffsetValue:
                builder.Append(dateTimeOffsetValue.ToString("o"));
                break;
#if NET6_0_OR_GREATER
            case DateOnly dateOnlyValue:
                builder.Append(dateOnlyValue.ToString("o"));
                break;
            case TimeOnly timeOnlyValue:
                builder.Append(timeOnlyValue.ToString("o"));
                break;
#endif
            case byte[] binaryValue:
                builder.AppendBytes(binaryValue);
                break;
            default:
                builder.Append(Convert.ToString(parameterValue, CultureInfo.InvariantCulture));
                break;
        }

        builder.Append('\'');
    }

    public static string FormatParameterValue(object? parameterValue, bool shouldLogParameterValue)
    {
        if (!shouldLogParameterValue)
        {
            return LoggingConstants.SENSITIVE_VALUE_PLACEHOLDER;
        }

        if (parameterValue is null)
        {
            return "null";
        }

        var builder = new StringBuilder();

        builder.Append('\'');

        switch (parameterValue)
        {
            case DateTime dateTimeValue:
                builder.Append(dateTimeValue.ToString("s"));
                break;
            case DateTimeOffset dateTimeOffsetValue:
                builder.Append(dateTimeOffsetValue.ToString("o"));
                break;
#if NET6_0_OR_GREATER
            case DateOnly dateOnlyValue:
                builder.Append(dateOnlyValue.ToString("o"));
                break;
            case TimeOnly timeOnlyValue:
                builder.Append(timeOnlyValue.ToString("o"));
                break;
#endif
            case byte[] binaryValue:
                builder.AppendBytes(binaryValue);
                break;
            default:
                builder.Append(Convert.ToString(parameterValue, CultureInfo.InvariantCulture));
                break;
        }

        builder.Append('\'');

        return builder.ToString();
    }

    public static string FormatExecutionTime(TimeSpan timeSpan)
    {
        if (timeSpan.Seconds > 0)
            return $"{timeSpan.Seconds}s";
#if NET7_0_OR_GREATER
        if (timeSpan.Milliseconds > 0)
            return $"{timeSpan.Milliseconds}ms";
        return $"{timeSpan.Microseconds}us";
#else
        return $"{timeSpan.Milliseconds}ms";
#endif
    }
}
