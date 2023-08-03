namespace Microsoft.Extensions.DependencyInjection;

public class SurrealDbOptionsBuilder
{
	private string? _endpoint;
	private string? _namespace;
	private string? _database;
	private string? _username;
	private string? _password;

	/// <summary>
	/// Parses the connection string and set the configuration accordingly.
	/// </summary>
	/// <param name="connectionString">Connection string to connect to a SurrealDB instance.</param>
	/// <exception cref="ArgumentException"></exception>
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

	public SurrealDbOptions Build()
	{
		return new SurrealDbOptions
		{
			Endpoint = _endpoint,
			Namespace = _namespace,
			Database = _database,
			Username = _username,
			Password = _password
		};
	}
}
