using Microsoft.Extensions.Options;
using SurrealDB.NET.Http;
using SurrealDB.NET.Rpc;

namespace SurrealDB.NET;

public sealed partial class SurrealClient : ISurrealClient, IDisposable
{
	private readonly ISurrealHttpClient _httpClient;
	private readonly ISurrealRpcClient _textClient;
	private SurrealOptions _options;
	private IDisposable? _changeToken;

	private string? _namespace;
	private string? _database;
	private string? _token;

	public string Namespace => string.IsNullOrWhiteSpace(_namespace) ? _options.DefaultNamespace : _namespace;
	public string Database => string.IsNullOrWhiteSpace(_database) ? _options.DefaultDatabase : _database;

	public SurrealClient(IOptionsMonitor<SurrealOptions> options, ISurrealHttpClient httpClient, ISurrealRpcClient textClient)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(httpClient);
		ArgumentNullException.ThrowIfNull(textClient);

		_textClient = textClient;
		_httpClient = httpClient;
		_options = options.CurrentValue;
		_changeToken = options.OnChange(o => _options = o);
	}

	public void Dispose()
	{
		_changeToken?.Dispose();
	}

	public async Task UseAsync(string @namespace, string database, CancellationToken ct = default)
	{
		_namespace = @namespace;
		_database = database;

		await _textClient.UseAsync(Namespace, Database, ct).ConfigureAwait(false);
	}

	public async Task ImportAsync(Stream source, CancellationToken ct = default)
	{
		await _httpClient.ImportAsync(source, ct).ConfigureAwait(false);
	}

    public async Task ExportAsync(Stream destination, CancellationToken ct = default)
    {
		await _httpClient.ExportAsync(destination, ct).ConfigureAwait(false);
    }

	public async Task<bool> HealthAsync(CancellationToken ct = default)
	{
		return await _httpClient.HealthAsync(ct).ConfigureAwait(false);
	}

    public async Task<bool> StatusAsync(CancellationToken ct = default)
    {
        return await _httpClient.StatusAsync(ct).ConfigureAwait(false);
    }

    public async Task<string> VersionAsync(CancellationToken ct = default)
    {
        return await _httpClient.VersionAsync(ct).ConfigureAwait(false);
    }

    public async Task LetAsync<T>(string name, T value, CancellationToken ct = default)
    {
		await _textClient.LetAsync(name, value, ct).ConfigureAwait(false);
    }

    public async Task UnsetAsync<T>(string name, CancellationToken ct = default)
    {
		await _textClient.UnsetAsync(name, ct).ConfigureAwait(false);
    }
}
