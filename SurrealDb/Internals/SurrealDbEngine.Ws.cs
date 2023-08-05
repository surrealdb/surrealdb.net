using SurrealDb.Exceptions;
using SurrealDb.Internals.Auth;
using SurrealDb.Internals.Helpers;
using SurrealDb.Internals.Json;
using SurrealDb.Internals.Models;
using SurrealDb.Internals.Ws;
using SurrealDb.Models;
using SurrealDb.Models.Auth;
using SurrealDb.Models.Response;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using Websocket.Client;

namespace SurrealDb.Internals;

internal class SurrealDbWsEngine : ISurrealDbEngine
{
	private static readonly ConcurrentDictionary<string, SurrealDbWsEngine> _wsEngines = new();
	private static readonly ConcurrentDictionary<string, SurrealWsTaskCompletionSource> _responseTasks = new();

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
		_wsClient = new WebsocketClient(_uri);

		_receiverSubscription = _wsClient.MessageReceived
			.ObserveOn(TaskPoolScheduler.Default)
			.Subscribe(message =>
			{
				if (message.MessageType == WebSocketMessageType.Text)
				{
					var response = JsonSerializer.Deserialize<ISurrealDbWsResponse>(message.Text, SurrealDbSerializerOptions.Default);

					if (_responseTasks.TryRemove(response!.Id, out var responseTaskCompletionSource))
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
				}

				if (message.MessageType == WebSocketMessageType.Binary)
				{
					// TODO
				}
			});
	}

    public async Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
	{
		await SendRequest("authenticate", new() { jwt.Token }, cancellationToken);
	}

	public void Configure(string? ns, string? db, string? username, string? password)
	{
		// 💡 Pre-configuration before connect
		if (ns is not null)
			_config.Use(ns, db);

		if (username is not null)
			_config.SetBasicAuth(username, password);
	}

	public async Task Connect(CancellationToken cancellationToken)
	{
		if (_wsClient.IsStarted)
			throw new SurrealDbException("Client already started");

		await _wsClient.StartOrFail();

		if (_config.Ns is not null)
			await Use(_config.Ns, _config.Db!, cancellationToken);

		if (_config.Auth is BasicAuth basicAuth)
			await SignIn(new RootAuth { Username = basicAuth.Username, Password = basicAuth.Password! }, cancellationToken);

		_config.Reset();
	}

	public async Task<T> Create<T>(T data, CancellationToken cancellationToken) where T : Record
	{
		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		var dbResponse = await SendRequest("create", new() { data.Id.ToString(), data }, cancellationToken);
		return dbResponse.GetValue<T>()!;
	}
	public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("create", new() { table, data }, cancellationToken);

		var list = dbResponse.GetValue<List<T>>() ?? new();
		return list.First();
	}

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
		await SendRequest("delete", new() { table }, cancellationToken);
	}
    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("delete", new() { thing.ToString() }, cancellationToken);

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

	public async Task Invalidate(CancellationToken cancellationToken)
	{
		await SendRequest("invalidate", null, cancellationToken);
	}

	public async Task<TOutput> Merge<TMerge, TOutput>(TMerge data, CancellationToken cancellationToken) where TMerge : Record
	{
		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		var dbResponse = await SendRequest("merge", new() { data.Id.ToString(), data }, cancellationToken);
		return dbResponse.GetValue<TOutput>()!;
	}
	public async Task<T> Merge<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("merge", new() { thing, data }, cancellationToken);
		return dbResponse.GetValue<T>()!;
	}

	public async Task<SurrealDbResponse> Query(
		string query,
		IReadOnlyDictionary<string, object> parameters,
		CancellationToken cancellationToken
	)
	{
		var dbResponse = await SendRequest("query", new() { query, parameters }, cancellationToken);

		var list = dbResponse.GetValue<List<ISurrealDbResult>>() ?? new();
		return new SurrealDbResponse(list);
	}

    public async Task<List<T>> Select<T>(string table, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("select", new() { table }, cancellationToken);
		return dbResponse.GetValue<List<T>>()!;
	}
    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("select", new() { thing.ToString() }, cancellationToken);
		return dbResponse.GetValue<T?>();
	}

    public async Task Set(string key, object value, CancellationToken cancellationToken)
	{
		await SendRequest("let", new() { key, value }, cancellationToken);
	}

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
	{
		await SendRequest("signin", new() { rootAuth }, cancellationToken);
	}
	public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("signin", new() { nsAuth }, cancellationToken);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}
	public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("signin", new() { dbAuth }, cancellationToken);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}
	public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		var dbResponse = await SendRequest("signin", new() { scopeAuth }, cancellationToken);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}

	public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		var dbResponse = await SendRequest("signup", new() { scopeAuth }, cancellationToken);
		var token = dbResponse.GetValue<string>()!;

		return new Jwt { Token = token! };
	}

	public Task Unset(string key, CancellationToken cancellationToken)
	{
		return SendRequest("unset", new() { key }, cancellationToken);
	}

	public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken) where T : Record
	{
		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		var dbResponse = await SendRequest("update", new() { data.Id.ToString(), data }, cancellationToken);
		return dbResponse.GetValue<T>()!;
	}

	public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
		await SendRequest("use", new() { ns, db }, cancellationToken);
	}

	public async Task<string> Version(CancellationToken cancellationToken)
	{
		var dbResponse = await SendRequest("version", null, cancellationToken);
		return dbResponse.GetValue<string>()!;
	}

	private async Task<SurrealDbWsOkResponse> SendRequest(string method, List<object?>? parameters, CancellationToken cancellationToken)
	{
		if (!_wsClient.IsStarted)
		{
			await Connect(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
		}

		var taskCompletionSource = new SurrealWsTaskCompletionSource(in _id);

		string id;

		// 💡 Ensures unique id (no collision)
		do
		{
			id = RandomHelper.CreateRandomId();
		} while (!_responseTasks.TryAdd(id, taskCompletionSource));

		var request = new Dictionary<string, object>
		{
			{ "id", id },
			{ "method", method },
		};

		if (parameters is not null && parameters.Count > 0)
			request.Add("params", parameters);

		string payload = JsonSerializer.Serialize(request, SurrealDbSerializerOptions.Default);
		_wsClient.Send(payload);

		var response = await taskCompletionSource.Task;
		cancellationToken.ThrowIfCancellationRequested();

		return response;
	}
}
