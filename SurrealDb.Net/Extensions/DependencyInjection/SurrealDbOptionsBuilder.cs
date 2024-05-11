using System.Text.Json;
using SurrealDb.Net.Internals.Constants;

namespace Microsoft.Extensions.DependencyInjection;

public class SurrealDbOptionsBuilder
{
    private string? _endpoint;
    private string? _namespace;
    private string? _database;
    private string? _username;
    private string? _password;
    private string? _token;
    private string? _namingPolicy;
    private string? _serialization;

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
                    throw new ArgumentException($"Invalid connection string: {connectionString}");

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
                case "server":
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
                case "serialization":
                    _serialization = value;
                    break;
            }
        }

        return this;
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

    public SurrealDbOptionsBuilder WithSerialization(string serialization)
    {
        EnsuresSerialization(serialization);

        _serialization = serialization;
        return this;
    }

    private static void EnsuresSerialization(string serialization)
    {
        if (string.IsNullOrWhiteSpace(serialization))
        {
            return;
        }

        string[] validSerializations = [SerializationConstants.JSON, SerializationConstants.CBOR];

        if (validSerializations.Contains(serialization.ToLowerInvariant()))
        {
            return;
        }

        throw new ArgumentException(
            $"Invalid serialization: {serialization}",
            nameof(serialization)
        );
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
            Serialization = _serialization
        };
    }
}
