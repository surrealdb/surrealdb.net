using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Constants;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class SurrealDbOptionsBuilder
{
    private string? _endpoint;
    private string? _namespace;
    private string? _database;
    private string? _username;
    private string? _password;
    private string? _token;
    private string? _namingPolicy;
    private bool _sensitiveDataLoggingEnabled;

    /// <summary>
    /// Parses the connection string and set the configuration accordingly.
    /// </summary>
    /// <param name="connectionString">Connection string to connect to a SurrealDB instance.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbOptionsBuilder FromConnectionString(string connectionString)
    {
        bool isPossibleSurroundedPassword = false;
        char? expectedPasswordLastChar = null;

        int currentPropertyNameStartIndex = -1;
        int currentPropertyNameEndIndex = -1;
        int currentPropertyValueStartIndex = -1;
        int currentPropertyValueEndIndex = -1;

        int index = 0;

        var span = connectionString.AsSpan();

        while (index < span.Length)
        {
            if (currentPropertyNameStartIndex == -1)
            {
                currentPropertyNameStartIndex = index;
                index++;
                continue;
            }

            char currentChar = span[index];

            if (currentPropertyNameEndIndex == -1)
            {
                if (currentChar == '=')
                {
                    currentPropertyNameEndIndex = index - 1;

                    ReadOnlySpan<char> currentPropertyName = span.Slice(
                        currentPropertyNameStartIndex,
                        currentPropertyNameEndIndex - currentPropertyNameStartIndex + 1
                    );
                    isPossibleSurroundedPassword =
                        currentPropertyName.Equals("password", StringComparison.OrdinalIgnoreCase)
                        || currentPropertyName.Equals("pass", StringComparison.OrdinalIgnoreCase);
                }

                index++;
                continue;
            }

            if (currentPropertyValueStartIndex == -1)
            {
                currentPropertyValueStartIndex = index;
                isPossibleSurroundedPassword &= currentChar is '\'' or '"' or '{';
                if (isPossibleSurroundedPassword)
                {
                    expectedPasswordLastChar = currentChar switch
                    {
                        '\'' or '"' => currentChar,
                        '{' => '}',
                        _ => throw new InvalidOperationException(),
                    };
                }

                index++;
                continue;
            }

            if (expectedPasswordLastChar.HasValue)
            {
                if (currentChar == expectedPasswordLastChar.Value)
                {
                    currentPropertyValueEndIndex = index;

                    TrySetProperty(ref span);
                    ResetForNextIteration();

                    index++; // 💡 Move to the next possible ';' so the next "index++" will move correctly to the next property
                }
            }
            else
            {
                if (currentChar == ';')
                {
                    currentPropertyValueEndIndex = index - 1;

                    TrySetProperty(ref span);
                    ResetForNextIteration();
                }
            }

            index++;
        }

        if (currentPropertyValueStartIndex != -1 && currentPropertyValueEndIndex == -1)
        {
            currentPropertyValueEndIndex = index - 1;
        }

        TrySetProperty(ref span);

        return this;

        void TrySetProperty(ref ReadOnlySpan<char> span)
        {
            if (currentPropertyNameStartIndex == -1)
            {
                return;
            }

            if (currentPropertyNameStartIndex != -1 && currentPropertyNameEndIndex == -1)
            {
                throw new ArgumentException(
                    $"Invalid connection string: {connectionString}",
                    nameof(connectionString)
                );
            }

            if (currentPropertyValueStartIndex == currentPropertyValueEndIndex)
            {
                return;
            }

            ReadOnlySpan<char> key = span.Slice(
                currentPropertyNameStartIndex,
                currentPropertyNameEndIndex - currentPropertyNameStartIndex + 1
            );
            ReadOnlySpan<char> valueSpan = span.Slice(
                currentPropertyValueStartIndex,
                currentPropertyValueEndIndex - currentPropertyValueStartIndex + 1
            );

            if (valueSpan.IsEmpty || valueSpan.IsWhiteSpace())
            {
                return;
            }

            if (key.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
            {
                string value = valueSpan.ToString();
                EnsuresCorrectEndpoint(value, nameof(connectionString));
                _endpoint = value;
                return;
            }
            if (key.Equals("server", StringComparison.OrdinalIgnoreCase))
            {
                string value = valueSpan.ToString();
                EnsuresCorrectServerEndpoint(value, nameof(connectionString));
                _endpoint = value;
                return;
            }
            if (key.Equals("client", StringComparison.OrdinalIgnoreCase))
            {
                string value = valueSpan.ToString();
                EnsuresCorrectClientEndpoint(value, nameof(connectionString));
                _endpoint = value;
                return;
            }
            if (
                key.Equals("namespace", StringComparison.OrdinalIgnoreCase)
                || key.Equals("ns", StringComparison.OrdinalIgnoreCase)
            )
            {
                _namespace = valueSpan.ToString();
                return;
            }
            if (
                key.Equals("database", StringComparison.OrdinalIgnoreCase)
                || key.Equals("db", StringComparison.OrdinalIgnoreCase)
            )
            {
                _database = valueSpan.ToString();
                return;
            }
            if (
                key.Equals("username", StringComparison.OrdinalIgnoreCase)
                || key.Equals("user", StringComparison.OrdinalIgnoreCase)
            )
            {
                _username = valueSpan.ToString();
                return;
            }
            if (
                key.Equals("password", StringComparison.OrdinalIgnoreCase)
                || key.Equals("pass", StringComparison.OrdinalIgnoreCase)
            )
            {
                _password = ExtractPassword(ref valueSpan);
                return;
            }
            if (key.Equals("token", StringComparison.OrdinalIgnoreCase))
            {
                _token = valueSpan.ToString();
                return;
            }
            if (key.Equals("namingPolicy", StringComparison.OrdinalIgnoreCase))
            {
                string value = valueSpan.ToString();
                EnsuresCorrectNamingPolicy(value, nameof(connectionString));
                _namingPolicy = value;
            }
        }

        void ResetForNextIteration()
        {
            isPossibleSurroundedPassword = false;
            expectedPasswordLastChar = null;

            currentPropertyNameStartIndex = -1;
            currentPropertyNameEndIndex = -1;
            currentPropertyValueStartIndex = -1;
            currentPropertyValueEndIndex = -1;
        }
    }

    private static void EnsuresCorrectEndpoint(string? endpoint, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint is required", argumentName);
        }

        if (SurrealDbOptionsValidation.IsValidEndpoint(endpoint))
        {
            return;
        }

        throw new ArgumentException($"Invalid endpoint: {endpoint}", argumentName);
    }

    private static void EnsuresCorrectServerEndpoint(string? endpoint, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Server endpoint is required", argumentName);
        }

        if (SurrealDbOptionsValidation.IsValidServerEndpoint(endpoint))
        {
            return;
        }

        throw new ArgumentException($"Invalid server endpoint: {endpoint}", argumentName);
    }

    private static void EnsuresCorrectClientEndpoint(string? endpoint, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Client endpoint is required", argumentName);
        }

        if (SurrealDbOptionsValidation.IsValidClientEndpoint(endpoint))
        {
            return;
        }

        throw new ArgumentException($"Invalid client endpoint: {endpoint}", argumentName);
    }

    public SurrealDbOptionsBuilder WithEndpoint(string? endpoint)
    {
        _endpoint = endpoint;
        return this;
    }

    public SurrealDbOptionsBuilder WithNamespace(string? ns)
    {
        _namespace = ns;
        return this;
    }

    public SurrealDbOptionsBuilder WithDatabase(string? db)
    {
        _database = db;
        return this;
    }

    public SurrealDbOptionsBuilder WithUsername(string? username)
    {
        _username = username;
        return this;
    }

    public SurrealDbOptionsBuilder WithPassword(string? password)
    {
        _password = password;
        return this;
    }

    /// <summary>
    /// Extract password stored in a connection string by removing surrounding when found.
    /// A password can surrounded by single quotes, double quotes or even curly brackets (e.g. "{my-password}").
    /// </summary>
    private static string ExtractPassword(ref ReadOnlySpan<char> value)
    {
        if (value.Length < 2)
        {
            return value.ToString();
        }

        char firstChar = value[0];
        char lastChar = value[^1];

        if (
            (firstChar == lastChar && firstChar is '\'' or '"')
            || (firstChar == '{' && lastChar == '}')
        )
        {
            return value[1..^1].ToString();
        }

        return value.ToString();
    }

    public SurrealDbOptionsBuilder WithToken(string? token)
    {
        _token = token;
        return this;
    }

    public SurrealDbOptionsBuilder WithNamingPolicy(string namingPolicy)
    {
        EnsuresCorrectNamingPolicy(namingPolicy, nameof(namingPolicy));

        _namingPolicy = namingPolicy;
        return this;
    }

    private static void EnsuresCorrectNamingPolicy(string namingPolicy, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(namingPolicy))
        {
            return;
        }

        if (SurrealDbOptionsValidation.IsValidNamingPolicy(namingPolicy))
        {
            return;
        }

        throw new ArgumentException($"Invalid naming policy: {namingPolicy}", argumentName);
    }

    /// <summary>
    /// Enables application data to be included in logs.
    /// This typically include parameter values set for SURQL queries, and parameters sent via any client methods.
    /// You should only enable this flag if you have the appropriate security measures in place based on the sensitivity of this data.
    /// </summary>
    /// <param name="sensitiveDataLoggingEnabled">If <c>true</c>, then sensitive data is logged.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public SurrealDbOptionsBuilder EnableSensitiveDataLogging(
        bool sensitiveDataLoggingEnabled = true
    )
    {
        _sensitiveDataLoggingEnabled = sensitiveDataLoggingEnabled;
        return this;
    }

    public SurrealDbOptions Build()
    {
        return new SurrealDbOptions
        {
            Endpoint = _endpoint,
            Namespace = _namespace,
            Database = _database,
            Username = _username,
            Password = _password,
            Token = _token,
            NamingPolicy = _namingPolicy,
            Logging = new() { SensitiveDataLoggingEnabled = _sensitiveDataLoggingEnabled },
        };
    }
}
