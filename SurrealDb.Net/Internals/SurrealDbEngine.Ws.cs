using Microsoft.IO;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.Response;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using Websocket.Client;

namespace SurrealDb.Net.Internals;

internal class SurrealDbWsEngine : ISurrealDbEngine
{
	private static readonly ConcurrentDictionary<string, SurrealDbWsEngine> _wsEngines = new();
	private static readonly ConcurrentDictionary<string, SurrealWsTaskCompletionSource> _responseTasks = new();
	private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();

	private readonly string _id;
    private readonly Uri _uri;
	private readonly SurrealDbWsEngineConfig _config = new();
	private readonly WebsocketClient _wsClient;
	private readonly IDisposable _receiverSubscription;

	public SurrealDbWsEngine(Uri uri)
    {
		string id;

		// 💡 Ensures unique id (no collision)
		do
		{
			id = RandomHelper.CreateRandomId();
		} while (!_wsEngines.TryAdd(id, this));

		_id = id;
		_uri = uri;
		_wsClient = new WebsocketClient(_uri)
		{
			IsTextMessageConversionEnabled = false
		};

		_receiverSubscription = _wsClient.MessageReceived
			.ObserveOn(TaskPoolScheduler.Default)
			.Select(message =>
				Observable.FromAsync(async (cancellationToken) =>
				{
					ISurrealDbWsResponse? response = null;

					switch (message.MessageType)
					{
						case WebSocketMessageType.Text:
							response = JsonSerializer.Deserialize<ISurrealDbWsResponse>(message.Text!, SurrealDbSerializerOptions.Default);
							break;
						case WebSocketMessageType.Binary:
							{
								using var stream = _memoryStreamManager.GetStream(message.Binary);

								response = await JsonSerializer.DeserializeAsync<ISurrealDbWsResponse>(
									stream,
									SurrealDbSerializerOptions.Default,
									cancellationToken
								).ConfigureAwait(false);
								break;
							}
					}

					if (response is not null && _responseTasks.TryRemove(response.Id, out var responseTaskCompletionSource))
					{
						switch (response)
						{
							case SurrealDbWsOkResponse okResponse:
								responseTaskCompletionSource.SetResult(okResponse);
								break;
							case SurrealDbWsErrorResponse errorResponse:
								responseTaskCompletionSource.SetException(new SurrealDbException(errorResponse.Error.Message));
								break;
							default:
								responseTaskCompletionSource.SetException(new SurrealDbException("Unknown response type"));
								break;
						}
					}
				})
			)
			.Merge()
			.Subscribe();
	}

    public async Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
	{
		await SendRequest("authenticate", new() { jwt.Token }, cancellationToken).ConfigureAwait(false);
	}

	public void Configure(string? ns, string? db, string? username, string? password)
	{
		// 💡 Pre-configuration before connect
		if (ns is not null)
			_config.Use(ns, db);

		if (username is not null)
			_config.SetBasicAuth(username, password);
	}
	public void Configure(string? ns, string? db, string? token = null)
	{
		// 💡 Pre-configuration before connect
		if (ns is not null)
			_config.Use(ns, db);

		if (token is not null)
			_config.SetBearerAuth(token);
	}

	public async Task Connect(CancellationToken cancellationToken)
	{
		if (_wsClient.IsStarted)
			throw new SurrealDbException("Client already started");

		await _wsClient.StartOrFail().ConfigureAwait(false);

		if (_config.Ns is not null)
			await Use(_config.Ns, _config.Db!, cancellationToken).ConfigureAwait(false);

		if (_config.Auth is BasicAuth basicAuth)
			await SignIn(new RootAuth { Username = basicAuth.Username, Password = basicAuth.Password! }, cancellationToken).ConfigureAwait(false);

		if (_config.Auth is BearerAuth bearerAuth)
			await Authenticate(new Jwt { Token = bearerAuth.Token }, cancellationToken).ConfigureAwait(false);

		_config.Reset();
	}

	public async Task<T> Create<T>(T data, CancellationToken cancellationToken) where T : Record
	{
		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		var dbResponse = await SendRequest("create", new() { data.Id.ToString(), data }, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<T>()!;
	}
	public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("create", new() { table, data }, cancellationToken).ConfigureAwait(false);

		var list = dbResponse.GetValue<List<T>>() ?? new();
		return list.First();
	}

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
		await SendRequest("delete", new() { table }, cancellationToken).ConfigureAwait(false);
	}
    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("delete", new() { thing.ToString() }, cancellationToken).ConfigureAwait(false);

		var valueKind = dbResponse.Result.ValueKind;
		return valueKind != JsonValueKind.Null && valueKind != JsonValueKind.Undefined;
	}

	public void Dispose()
	{
		_wsClient.Stop(WebSocketCloseStatus.NormalClosure, "Client disposed");
		_receiverSubscription.Dispose();

		foreach (var (key, value) in _responseTasks)
		{
			if (value.WsEngineId == _id && _responseTasks.TryRemove(key, out var responseTaskCompletionSource))
			{
				responseTaskCompletionSource.SetCanceled();
			}
		}

		_wsEngines.TryRemove(_id, out _);

		_wsClient.Dispose();
	}

	public async Task<bool> Health(CancellationToken cancellationToken)
	{
		if (_wsClient.IsStarted)
			return true;

		try
		{
			await _wsClient.StartOrFail().ConfigureAwait(false);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public async Task Invalidate(CancellationToken cancellationToken)
	{
		await SendRequest("invalidate", null, cancellationToken).ConfigureAwait(false);
	}

	public async Task<TOutput> Merge<TMerge, TOutput>(TMerge data, CancellationToken cancellationToken) where TMerge : Record
	{
		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		var dbResponse = await SendRequest("merge", new() { data.Id.ToWsString(), data }, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<TOutput>()!;
	}
	public async Task<T> Merge<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("merge", new() { thing.ToWsString(), data }, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<T>()!;
	}

	public async Task<SurrealDbResponse> Query(
		string query,
		IReadOnlyDictionary<string, object> parameters,
		CancellationToken cancellationToken
	)
	{
		var dbResponse = await SendRequest("query", new() { query, parameters }, cancellationToken).ConfigureAwait(false);

		var list = dbResponse.GetValue<List<ISurrealDbResult>>() ?? new();
		return new SurrealDbResponse(list);
	}

    public async Task<List<T>> Select<T>(string table, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("select", new() { table }, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<List<T>>()!;
	}
    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("select", new() { thing.ToWsString() }, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<T?>();
	}

    public async Task Set(string key, object value, CancellationToken cancellationToken)
	{
		await SendRequest("let", new() { key, value }, cancellationToken).ConfigureAwait(false);
	}

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
	{
		await SendRequest("signin", new() { rootAuth }, cancellationToken).ConfigureAwait(false);
	}
	public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("signin", new() { nsAuth }, cancellationToken).ConfigureAwait(false);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}
	public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("signin", new() { dbAuth }, cancellationToken).ConfigureAwait(false);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}
	public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		var dbResponse = await SendRequest("signin", new() { scopeAuth }, cancellationToken).ConfigureAwait(false);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}

	public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		var dbResponse = await SendRequest("signup", new() { scopeAuth }, cancellationToken).ConfigureAwait(false);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}

	public async Task Unset(string key, CancellationToken cancellationToken)
	{
		await SendRequest("unset", new() { key }, cancellationToken).ConfigureAwait(false);
	}

	public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken) where T : Record
	{
		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		var dbResponse = await SendRequest("update", new() { data.Id.ToWsString(), data }, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<T>()!;
	}

	public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
		await SendRequest("use", new() { ns, db }, cancellationToken).ConfigureAwait(false);
	}

	public async Task<string> Version(CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("version", null, cancellationToken).ConfigureAwait(false);
		return dbResponse.GetValue<string>()!;
	}

	private async Task<SurrealDbWsOkResponse> SendRequest(string method, List<object?>? parameters, CancellationToken cancellationToken)
	{
		if (!_wsClient.IsStarted)
		{
			await Connect(cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
		}

		var taskCompletionSource = new SurrealWsTaskCompletionSource(_id);

		string id;

		// 💡 Ensures unique id (no collision)
		do
		{
			id = RandomHelper.CreateRandomId();
		} while (!_responseTasks.TryAdd(id, taskCompletionSource));

		bool shouldSendParamsInRequest = parameters is not null && parameters.Count > 0;

		var request = new SurrealDbWsRequest
		{
			Id = id,
			Method = method,
			Parameters = shouldSendParamsInRequest ? parameters : null,
		};

		using var stream = _memoryStreamManager.GetStream();
		await JsonSerializer
			.SerializeAsync(stream, request, SurrealDbSerializerOptions.Default, cancellationToken)
			.ConfigureAwait(false);
		stream.Seek(0, SeekOrigin.Begin);
		using var reader = new StreamReader(stream);
		string payload = await reader.ReadToEndAsync().ConfigureAwait(false);

		_wsClient.Send(payload);

		var response = await taskCompletionSource.Task.ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();

		return response;
	}
}
