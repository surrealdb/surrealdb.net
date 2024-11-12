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
        var properties = connectionString
            .Split(";", StringSplitOptions.RemoveEmptyEntries)
            .Select(str =>
            {
                int separatorIndex = str.IndexOf("=", StringComparison.Ordinal);

                if (separatorIndex <= 0)
                    throw new ArgumentException(
                        $"Invalid connection string: {connectionString}",
                        nameof(connectionString)
                    );

                return new KeyValuePair<string, string>(
                    str[..separatorIndex],
                    str[(separatorIndex + 1)..]
                );
            });

        foreach (var (key, value) in properties)
        {
            switch (key.ToLowerInvariant())
            {
                case "endpoint":
                    _endpoint = value;
                    break;
                case "server":
                    EnsuresCorrectServerEndpoint(value, nameof(connectionString));
                    _endpoint = value;
                    break;
                case "client":
                    EnsuresCorrectClientEndpoint(value, nameof(connectionString));
                    _endpoint = value;
                    break;
                case "namespace":
                case "ns":
                    _namespace = value;
                    break;
                case "database":
                case "db":
                    _database = value;
                    break;
                case "username":
                case "user":
                    _username = value;
                    break;
                case "password":
                case "pass":
                    _password = value;
                    break;
                case "token":
                    _token = value;
                    break;
                case "namingpolicy":
                    _namingPolicy = value;
                    break;
            }
        }

        return this;
    }

    private static void EnsuresCorrectServerEndpoint(string? endpoint, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        string[] validServerEndpoints =
        [
            EndpointConstants.Server.HTTP,
            EndpointConstants.Server.HTTPS,
            EndpointConstants.Server.WS,
            EndpointConstants.Server.WSS
        ];
        string lowerEndpoint = endpoint.ToLowerInvariant();

        if (validServerEndpoints.Any(lowerEndpoint.StartsWith))
        {
            return;
        }

        throw new ArgumentException($"Invalid server endpoint: {endpoint}", argumentName);
    }

    private static void EnsuresCorrectClientEndpoint(string? endpoint, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        string[] validClientEndpoints =
        [
            EndpointConstants.Client.MEMORY,
            EndpointConstants.Client.ROCKSDB,
            EndpointConstants.Client.SURREALKV
        ];
        string lowerEndpoint = endpoint.ToLowerInvariant();

        if (validClientEndpoints.Any(lowerEndpoint.StartsWith))
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

    public SurrealDbOptionsBuilder WithToken(string? token)
    {
        _token = token;
        return this;
    }

    public SurrealDbOptionsBuilder WithNamingPolicy(string namingPolicy)
    {
        EnsuresCorrectNamingPolicy(namingPolicy);

        _namingPolicy = namingPolicy;
        return this;
    }

    private static void EnsuresCorrectNamingPolicy(string namingPolicy)
    {
        if (string.IsNullOrWhiteSpace(namingPolicy))
        {
            return;
        }

        string[] validNamingPolicies =
        [
            NamingPolicyConstants.CAMEL_CASE,
            NamingPolicyConstants.SNAKE_CASE,
            NamingPolicyConstants.SNAKE_CASE_LOWER,
            NamingPolicyConstants.SNAKE_CASE_UPPER,
            NamingPolicyConstants.KEBAB_CASE,
            NamingPolicyConstants.KEBAB_CASE_LOWER,
            NamingPolicyConstants.KEBAB_CASE_UPPER
        ];

        if (validNamingPolicies.Contains(namingPolicy.ToLowerInvariant()))
        {
            return;
        }

        throw new ArgumentException($"Invalid naming policy: {namingPolicy}", nameof(namingPolicy));
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
            Logging = new() { SensitiveDataLoggingEnabled = _sensitiveDataLoggingEnabled }
        };
    }
}
