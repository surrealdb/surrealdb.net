namespace SurrealDb.Internals.Http;

internal class HttpClientWrapper : IDisposable
{
	private readonly bool _shouldDispose;

	public HttpClient Instance { get; }

	public HttpClientWrapper(HttpClient httpClient, bool shouldDispose)
	{
		Instance = httpClient;
		_shouldDispose = shouldDispose;
	}

	public void Dispose()
	{
		if (_shouldDispose)
			Instance.Dispose();
	}
}
