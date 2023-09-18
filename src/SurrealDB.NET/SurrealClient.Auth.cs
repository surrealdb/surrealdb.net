namespace SurrealDB.NET;

// TODO: HTTP fallbacks where applicable

public sealed partial class SurrealClient
{
	public async Task<TScope?> InfoAsync<TScope>(CancellationToken ct = default)
	{
		return await _textClient.InfoAsync<TScope>(ct).ConfigureAwait(false);
	}

	public async Task<string> SignupAsync<TScope>(
		string @namespace,
		string database,
		string scope,
		TScope user,
		CancellationToken ct = default)
	{
		return await _textClient.SignupAsync(@namespace, database, scope, user, ct)
			.ConfigureAwait(false);
	}

	public async Task<string> SignupAsync<TScope>(
		string scope,
		TScope user,
		CancellationToken ct = default)
	{
		return await _textClient.SignupAsync(Namespace, Database, scope, user, ct)
			.ConfigureAwait(false);
	}

	public async Task SigninRootAsync(
		string username,
		string password,
		CancellationToken ct = default)
	{
		_token = await _textClient.SigninRootAsync(username, password, ct)
			.ConfigureAwait(false);
		await _textClient.AuthenticateAsync(_token, ct).ConfigureAwait(false);
	}

	public async Task SigninNamespaceAsync(
		string username,
		string password,
		CancellationToken ct = default)
	{
		_token = await _textClient.SigninNamespaceAsync(Namespace, username, password, ct)
			.ConfigureAwait(false);
        await _textClient.AuthenticateAsync(_token, ct).ConfigureAwait(false);
    }

	public async Task SigninAsync<TScope>(
		string @namespace,
		string database,
		string scope,
		TScope user,
		CancellationToken ct = default)
	{
		_token = await _textClient.SigninAsync(@namespace, database, scope, user, ct)
			.ConfigureAwait(false);
        await _textClient.AuthenticateAsync(_token, ct).ConfigureAwait(false);
    }

	public async Task SigninAsync<TScope>(
		string scope,
		TScope user,
		CancellationToken ct = default)
	{
		await SigninAsync(Namespace, Database, scope, user, ct).ConfigureAwait(false);
	}

	public async Task Authenticate(string token, CancellationToken ct = default)
	{
		await _textClient.AuthenticateAsync(token, ct).ConfigureAwait(false);
	}
}
