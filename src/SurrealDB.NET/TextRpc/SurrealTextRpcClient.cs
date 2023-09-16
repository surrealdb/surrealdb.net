using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SurrealDB.NET.Json;

namespace SurrealDB.NET.TextRpc;

internal readonly struct SurrealTextRpcRequest() : IDisposable
{
    public required ArrayPoolBufferWriter<byte> Buffer { get; init; }

    public required SurrealOptions Options { get; init; }

    public Guid Id { get; } = Guid.NewGuid();

    public void Dispose()
    {
        Buffer.Dispose();
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonDocument>(Buffer.WrittenSpan), Options.JsonRequestOptions);
    }
}

internal readonly struct SurrealTextRpcResponse()
{
    public required ReadOnlyMemory<byte> Buffer { get; init; }

    public required SurrealOptions Options { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonDocument>(Buffer.Span), Options.JsonResponseOptions);
    }
}

public sealed partial class SurrealTextRpcClient : ISurrealClient
{
    private ClientWebSocket _ws = new();
    private readonly CancellationTokenSource _cts = new();
    private SurrealOptions _options;
    private readonly IDisposable? _optionsChangeToken;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<SurrealTextRpcResponse>> _responseHandlers = new();
    private readonly ILogger<SurrealTextRpcClient> _logger;
    private Task? _listener;

    public SurrealTextRpcClient(
        [NotNull] IOptionsMonitor<SurrealOptions> options,
        [NotNull] ILogger<SurrealTextRpcClient> logger)
    {
        _logger = logger;
        _ws = new ClientWebSocket();
        _options = options.CurrentValue;
        _optionsChangeToken = options.OnChange(o => _options = o);
    }

    /// <summary>
    /// This method specifies the namespace and database for the current connection.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#use"/> for more information.
    /// </remarks>
    /// <param name="namespace">Specifies the namespace to use.</param>
    /// <param name="database">Specifies the database to use.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/>.</returns>
    public async Task UseAsync(string @namespace, string database, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("use"u8, (@namespace, database), static (p, state) =>
        {
            p.WriteStringValue(state.@namespace);
            p.WriteStringValue(state.database);
        });

        _ = await SendAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// This method returns the record of an authenticated scope user..
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#info"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The entity type for the scope authenticated identity.</typeparam>
    /// <param name="ct">A token to cance the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain the scope authenticated identity, or <see langword="null"/>.</returns>
    public async Task<T?> InfoAsync<T>(CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("info"u8);
        var response = await SendAsync(request, ct).ConfigureAwait(false);
        return ParseSingle<T>(response.Buffer.Span);
    }

    /// <summary>
    /// This method allows you to signup a user against a scope's <c>SIGNUP</c> method.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#signup" /> for more information.
    /// </remarks>
    /// <typeparam name="T">The entity type for the scope identity.</typeparam>
    /// <param name="namespace">Specifies the namespace of the scope.</param>
    /// <param name="database">Specifies the database of the scope.</param>
    /// <param name="scope">Specifies the scope.</param>
    /// <param name="identity">Specifies any variables used by the scope's <c>SIGNUP</c> method.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain a bearer token if sign up was succesful.</returns>
    public async Task<string> SignupAsync<T>(string @namespace, string database, string scope, T identity, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("signup"u8, (@namespace, database, scope, identity, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStartObject();

            p.WriteString("NS"u8, state.@namespace);
            p.WriteString("DB"u8, state.database);
            p.WriteString("SC"u8, state.scope);

            var ext = JsonSerializer.SerializeToElement<T>(state.identity, state.JsonRequestOptions);
            foreach (var prop in ext.EnumerateObject())
            {
                p.WritePropertyName(prop.Name);
                prop.Value.WriteTo(p);
            }

            p.WriteEndObject();
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        return ParseSingle<string>(response.Buffer.Span)!;
    }

    /// <summary>
    /// Signs in using a root user.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#signin"/> for more information.
    /// </remarks>
    /// <param name="username">The root username.</param>
    /// <param name="password">The root password.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain a bearer token if sign in was succesful.</returns>
    public async Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("signin"u8, (username, password), static (p, state) =>
        {
            p.WriteStartObject();
            p.WriteString("user"u8, state.username);
            p.WriteString("pass"u8, state.password);
            p.WriteEndObject();
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);
        return ParseSingle<string>(response.Buffer.Span) ?? throw new SurrealException("Failed to deserialize token from json");
    }

    /// <summary>
    /// This method allows you to signin a SC user against SurrealDB.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#signin"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The entity type for the scope identity.</typeparam>
    /// <param name="namespace">The namespace to sign in to.</param>
    /// <param name="database">The database to sign in to.</param>
    /// <param name="scope">The scope to sign in to.</param>
    /// <param name="identity">Specifies any variables used by the scope's <c>SIGNIN</c> method.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain a bearer token if sign in was succesful.</returns>
    public async Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T identity, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("signin"u8, (@namespace, database, scope, identity, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStartObject();

            p.WriteString("NS"u8, state.@namespace);
            p.WriteString("DB"u8, state.database);
            p.WriteString("SC"u8, state.scope);

            var ext = JsonSerializer.SerializeToElement<T>(state.identity, state.JsonRequestOptions);

            foreach (var prop in ext.EnumerateObject())
            {
                p.WritePropertyName(prop.Name);
                prop.Value.WriteTo(p);
            }

            p.WriteEndObject();
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        return ParseSingle<string>(response.Buffer.Span) ?? throw new SurrealException("Failed to deserialize token from json");
    }

    /// <summary>
    /// This method allows you to authenticate a user against SurrealDB with a token.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#authenticate"/> for more information.
    /// </remarks>
    /// <param name="token">The token that authenticates the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/>.</returns>
    public async Task AuthenticateAsync(string token, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("authenticate"u8, token, static (p, state) =>
        {
            p.WriteStringValue(state);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// This method will invalidate the user's session for the current connection.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#invalidate"/> for more information.
    /// </remarks>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/>.</returns>
    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("invalidate"u8);

        _ = await SendAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// This method stores a variable on the current connection.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#let"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="name">The name for the variable without a prefixed <c>$</c> character.</param>
    /// <param name="value">The value for the variable.</param>
    /// <param name="ct">A token for cancelling the operation.</param>
    /// <returns>A running <see cref="Task"/>.</returns>
    public async Task LetAsync<T>(string name, T value, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("let"u8, (name, value, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStringValue(state.name);
			JsonSerializer.Serialize<T>(p, state.value, state.JsonRequestOptions);
        });

        _ = await SendAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// This method removes a variable from the current connection.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#unset"/> for more information.
    /// </remarks>
    /// <param name="name">The name of the variable without a prefixed <c>$</c> character.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns></returns>
    public async Task UnsetAsync(string name, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("unset"u8, name, static (p, state) =>
        {
            p.WriteStringValue(state);
        });

        _ = await SendAsync(request, ct).ConfigureAwait(false);
    }

    private readonly ConcurrentDictionary<SurrealLiveQueryId, Func<JsonElement, SurrealEventType, Task>> _liveQueryHandlers = new();

    /// <summary>
    /// This methods initiates a live query for a specified table name.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#live"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The record's mapped C# type.</typeparam>
    /// <param name="table">The table to initiate a live query for.</param>
    /// <param name="callback">An asynchronous handler for notifications on the live query</param>
    /// <param name="diff">If set to true, live notifications will contain an array of JSON Patches instead of the entire record</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain an id that can be matched to notifications.</returns>
    public async Task<SurrealLiveQueryId> LiveAsync<T>(string table, Func<T, SurrealEventType, Task> callback, bool diff = false, CancellationToken ct = default)

    {
        using var request = BuildJsonRequest("live"u8, (table, diff), static (p, state) =>
        {
            p.WriteStringValue(state.table);
            if (state.diff)
                p.WriteBooleanValue(true);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var liveId = new SurrealLiveQueryId(ParseSingle<Guid>(response.Buffer.Span));

        _liveQueryHandlers[liveId] = async (result, type) =>
        {
            var record = JsonSerializer.Deserialize<T>(result);
            await callback(record!, type).ConfigureAwait(false);
        };

        return liveId;
    }

    /// <inheritdoc cref="LiveAsync{T}(string, Func{T, SurrealEventType, Task}, bool, CancellationToken)"/>
    /// <param name="callback">A synchronous handler for notifications</param>
    public async Task<SurrealLiveQueryId> LiveAsync<T>(string table, Action<T, SurrealEventType> callback, bool diff = false, CancellationToken ct = default)

        => await LiveAsync<T>(table, (r, t) =>
        {
            callback(r, t);
            return Task.CompletedTask;

        }, diff, ct).ConfigureAwait(false);

    /// <summary>
    /// This methods kills an active live query.
    /// </summary>
    /// <remarks>
    /// See <see href="https://surrealdb.com/docs/integration/websocket/text#kill"/> for more information.
    /// </remarks>
    /// <param name="queryId">The UUID or <see cref="Guid"/> of the live query to kill.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/>.</returns>
    public async Task KillAsync(SurrealLiveQueryId queryId, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("kill"u8, queryId, static (p, state) =>
        {
            p.WriteStringValue(state.ToString());
        });

        try
        {
            if (!_liveQueryHandlers.TryRemove(queryId, out _))
                throw new SurrealException("Failed to remove live query handler");
        }
        finally
        {
            _ = await SendAsync(request, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// This method executes a custom query against SurrealDB.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://surrealdb.com/docs/integration/websocket/text#query"/> for more information.
    /// </remarks>
    /// <param name="sql">The query to execute against SurrealDB.</param>
    /// <param name="vars">A set of variables used by the query</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain the results of the query.</returns>
    public async Task<SurrealQueryResult> QueryAsync(string sql, object? vars = null, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("query"u8, (sql, vars, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStringValue(state.sql);

            if (state.vars is not null)
				JsonSerializer.Serialize(p, state.vars, state.JsonRequestOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        return new SurrealQueryResult(JsonSerializer.Deserialize<JsonElement>(response.Buffer.Span));
    }

	/// <summary>
	/// This method selects either all records in a table or a single record.
	/// </summary>
	/// <remarks>
	/// See <see cref="https://surrealdb.com/docs/integration/websocket/text#select"/> for more information.
	/// </remarks>
	/// <typeparam name="T">The entity type to map the results to.</typeparam>
	/// <param name="thing">The specific record to select.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <returns>A running <see cref="Task"/> that will contain the results of the select.</returns>
	public async Task<T?> SelectAsync<T>(Thing recordId, CancellationToken ct = default)
    {
		if (!recordId.IsSpecific)
			throw new SurrealException("Use SelectManyAsync to select a whole table");

		using var request = BuildJsonRequest("select"u8, recordId, static (p, state) =>
        {
            p.WriteThingValue(state);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

        return element.GetProperty("result"u8).Deserialize<T>();
    }

    /// <summary>
    /// This method selects either all records in a table or a single record.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://surrealdb.com/docs/integration/websocket/text#select"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The entity type to map the results to.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain the results of the select.</returns>
    public async Task<IEnumerable<T>> SelectManyAsync<T>(Thing table, CancellationToken ct = default)
    {
		if (table.IsSpecific)
			throw new SurrealException("Use SelectAsync to select a single record");

        using var request = BuildJsonRequest("select"u8, table, static (p, state) =>
        {
            p.WriteThingValue(state);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

        return element.GetProperty("result"u8).Deserialize<IEnumerable<T>>()!;
    }

    /// <summary>
    /// This method creates a record with the specified id.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://surrealdb.com/docs/integration/websocket/text#creates"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The record's mapped C# type.</typeparam>
    /// <param name="table">The table to create a record in.</param>
    /// <param name="data">The content of the record</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain the created record.</returns>
    public async Task<T> CreateAsync<T>(Thing id, T data, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("create"u8, (id, data, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteThingValue(state.id);
            p.WriteRecordValueWithoutId<T>(state.data, state.JsonRequestOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

		if (string.IsNullOrWhiteSpace(id.Id))
			return element.GetProperty("result"u8).EnumerateArray().First().Deserialize<T>()!;

		return element.GetProperty("result"u8).Deserialize<T>()!;
    }

	/// <summary>
	/// This method inserts one or multiple records in a table.
	/// </summary>
	/// <remarks>
	/// See <see cref="https://surrealdb.com/docs/integration/websocket/text#insert"/> for more information.
	/// </remarks>
	/// <typeparam name="T">The record's mapped C# type.</typeparam>
	/// <param name="table">The table to insert in to.</param>
	/// <param name="data">One or multiple record(s).</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <returns>A running <see cref="Task"/> that will contain the inserted records.</returns>
	public async Task<T> InsertAsync<T>(string table, T data, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("insert"u8, (table, data, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStringValue(state.table);
			JsonSerializer.Serialize<T>(p, state.data, state.JsonRequestOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var result = SurrealJson.BytesToJsonElement(response.Buffer.Span).GetProperty("result"u8);

        return data is System.Collections.IEnumerable
            ? result.Deserialize<T>()!
            : result.EnumerateArray().First().Deserialize<T>()!;        
    }

    /// <summary>
    /// This method replaces a specific record with specified data.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://surrealdb.com/docs/integration/websocket/text#update"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The record's mapped C# type.</typeparam>
    /// <param name="id">The id of the record.</param>
    /// <param name="data">The new content of the record.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain the updated records.</returns>
    public async Task<T> UpdateAsync<T>(Thing id, T data, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("update"u8, (id, data, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteThingValue(state.id);
            p.WriteRecordValueWithoutId<T>(state.data, state.JsonRequestOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

        return element.GetProperty("result"u8).Deserialize<T>()!;
    }

    /// <summary>
    /// This method replaces all records in a table with specified data.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://surrealdb.com/docs/integration/websocket/text#update"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The record's mapped C# type.</typeparam>
    /// <param name="table">The table to update.</param>
    /// <param name="data">The new content of all records.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will the updated records.</returns>
    public async Task<IEnumerable<T>> BulkUpdateAsync<T>(string table, T data, CancellationToken ct = default)
    { 
        using var request = BuildJsonRequest<(string table, T data, JsonSerializerOptions JsonSerializerOptions)>("update"u8, (table, data, JsonSerializerOptions: _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStringValue(state.table);
			JsonSerializer.Serialize<T>(p, state.data, state.JsonSerializerOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

        return element.GetProperty("result"u8).Deserialize<IEnumerable<T>>()!;
    }

	/// <summary>
	/// This method merges specified data into either all records in a table or a single record. If the specified record does not exist, it is created.
	/// </summary>
	/// <remarks>
	/// See <see cref="https://surrealdb.com/docs/integration/websocket/text#merge"/> for more information.
	/// </remarks>
	/// <typeparam name="T">The record's mapped C# type.</typeparam>
	/// <param name="thing">The id of the record.</param>
	/// <param name="merger">An anonymous object that represents the data to be merged.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <returns>A running <see cref="Task"/> that will contain the result of the merge.</returns>
	public async Task<T> MergeAsync<T>(Thing thing, object merger, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("merge"u8, (thing, merger, _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteThingValue(state.thing);
			JsonSerializer.Serialize(p, state.merger, state.JsonRequestOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

        return element.GetProperty("result"u8).Deserialize<T>(_options.JsonResponseOptions)!;
    }

    /// <summary>
    /// This method merges specified data into either all records in a table or a single record. If the specified record does not exist, it is created.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://surrealdb.com/docs/integration/websocket/text#merge"/> for more information.
    /// </remarks>
    /// <typeparam name="T">The record's mapped C# type.</typeparam>
    /// <param name="id">The id of the record.</param>
    /// <param name="merger">An anonymous object that represents the data to be merged.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A running <see cref="Task"/> that will contain the result of the merge.</returns>
    public async Task<IEnumerable<T>> MergeAsync<T>(string table, object merger, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("merge"u8, (table, merger, JsonSerializerOptions: _options.JsonRequestOptions), static (p, state) =>
        {
            p.WriteStringValue(state.table);
			JsonSerializer.Serialize(p, state.merger, state.JsonSerializerOptions);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        var element = SurrealJson.BytesToJsonElement(response.Buffer.Span);

        return element.GetProperty("result"u8).Deserialize<IEnumerable<T>>(_options.JsonResponseOptions)!;
    }

	/// <summary>
	/// This method patches either all records in a table or a single record with specified patches
	/// </summary>
	/// <typeparam name="T">The record's mapped C# type.</typeparam>
	/// <param name="thing">The record or table to patch.</param>
	/// <param name="patches">A builder on which patch operations can be applied.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <returns>A running <see cref="Task"/>.</returns>
	public async Task PatchAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default)
    {
        using var request = BuildJsonRequest("patch"u8, (thing, patches, _options.JsonRequestOptions), static (p, state) =>
		{
			p.WriteThingValue(state.thing);
			p.WriteStartArray();
			var builder = new SurrealJsonPatchBuilder<T>(p, state.JsonRequestOptions);
			state.patches?.Invoke(builder);
			p.WriteEndArray();
		});

        _ = await SendAsync(request, ct).ConfigureAwait(false);
    }

	/// <summary>
	/// This method deletes either all records in a table or a single record.
	/// </summary>
	/// <remarks>
	/// See <see cref="https://surrealdb.com/docs/integration/websocket/text#delete"/> for more information.
	/// </remarks>
	/// <typeparam name="T">The record's mapped C# type.</typeparam>
	/// <param name="thing">The record to delete</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <returns>A running <see cref="Task"/> that will contain the deleted record.</returns>
	public async Task<T?> DeleteAsync<T>(Thing thing, CancellationToken ct = default)
    {
		if (!thing.IsSpecific && !_options.AllowDeleteOnFullTable)
			throw new SurrealException($"Option '{nameof(_options.AllowDeleteOnFullTable)}' is disabled. Set this to true to allow full table deletion");

        using var request = BuildJsonRequest("delete"u8, thing, static (p, state) =>
        {
            p.WriteThingValue(state);
        });

        var response = await SendAsync(request, ct).ConfigureAwait(false);

        return ParseSingle<T>(response.Buffer.Span);
    }

    private static bool TryGetResponseIdFromJsonBuffer(in SurrealTextRpcResponse response, out Guid id)
    {
        var reader = new Utf8JsonReader(response.Buffer.Span);

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName && reader.ValueTextEquals("id"u8) && reader.CurrentDepth is 1)
            {
                if (reader.Read() && reader.TryGetGuid(out id))
                    return true;
            }
            else if (reader.CurrentDepth > 1)
            {
                reader.Skip();
            }
        }

        id = default;
        return false;
    }

    [LoggerMessage(EventId = 0, EventName = nameof(OnConnected), Level = LogLevel.Information, Message = "Connected to SurrealDB '{Endpoint}'")]
    static partial void OnConnected(ILogger logger, Uri endpoint);

    [LoggerMessage(EventId = -1, EventName = nameof(OnAttemptingToConnect), Level = LogLevel.Information, Message = "Connected to SurrealDB '{Endpoint}'")]
    static partial void OnAttemptingToConnect(ILogger logger, Uri endpoint);

    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (_ws.State is WebSocketState.Open)
            return;

        var endpoint = new Uri($"ws://{_options.Endpoint.Host}:{_options.Endpoint.Port}/rpc"); // TODO: wss://

        OnAttemptingToConnect(_logger, endpoint);
        await _ws.ConnectAsync(endpoint, ct).ConfigureAwait(false);
        OnConnected(_logger, endpoint);
        _listener = Task.Run(StartListening, _cts.Token);
    }

    [LoggerMessage(EventId = 1, EventName = nameof(OnRequestSent), Level = LogLevel.Debug, Message = "Sent request: {Request}")]
    static partial void OnRequestSent(ILogger logger, SurrealTextRpcRequest request);

    [LoggerMessage(EventId = 2, EventName = nameof(OnResponseReceived), Level = LogLevel.Debug, Message = "Received response: {Response}")]
    static partial void OnResponseReceived(ILogger logger, SurrealTextRpcResponse response);

    private async Task<SurrealTextRpcResponse> SendAsync(SurrealTextRpcRequest request, CancellationToken ct)
    {
        if (_ws.State is not WebSocketState.Open)
        {
            await OpenAsync(ct).ConfigureAwait(false);
            await UseAsync(_options.DefaultNamespace, _options.DefaultDatabase, ct).ConfigureAwait(false);
        }

        var tcs = new TaskCompletionSource<SurrealTextRpcResponse>();
        _responseHandlers[request.Id] = tcs;
        await _ws.SendAsync(request.Buffer.DangerousGetArray(), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        OnRequestSent(_logger, request);
        var response = await tcs.Task.ConfigureAwait(false);
        OnResponseReceived(_logger, response);
        ThrowOnError(response.Buffer.Span);
        return response;
    }

    [LoggerMessage(EventId = 3, EventName = nameof(OnError), Level = LogLevel.Debug, Message = "Received error response: {Error}")]
    static partial void OnError(ILogger logger, JsonDocument? error);

    private void ThrowOnError(ReadOnlySpan<byte> response)
    {
        var reader = new Utf8JsonReader(response);

        var isError = reader.Read()
            && reader.TokenType is JsonTokenType.StartObject
            && reader.Read()
            && reader.TokenType is JsonTokenType.PropertyName
            && reader.ValueTextEquals("error"u8);

        if (isError)
        {
            OnError(_logger, JsonSerializer.Deserialize<JsonDocument>(response));
            throw new SurrealException(JsonSerializer.Deserialize<SurrealError>(ref reader));
        }
    }

    private async Task StartListening()
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);

        while (_ws.State is WebSocketState.Open && !_cts.IsCancellationRequested)
        {
            try
            {
                using var stream = new MemoryStream();
                WebSocketReceiveResult received;
                do
                {
                    received = await _ws.ReceiveAsync(buffer, _cts.Token).ConfigureAwait(false);
                    await stream.WriteAsync(buffer.Array, buffer.Offset, received.Count, _cts.Token).ConfigureAwait(false);

                } while (!received.EndOfMessage);

                stream.Seek(0, SeekOrigin.Begin);

                if (received.MessageType is WebSocketMessageType.Binary)
                {

                }
                else if (received.MessageType is WebSocketMessageType.Text)
                {
                    await ProcessTextMessageAsync(new SurrealTextRpcResponse
                    {
                        Buffer = stream.ToArray(),
                        Options = _options
                    }).ConfigureAwait(false);
                }
                else if (received.MessageType is WebSocketMessageType.Close)
                {
                    await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Gracefully acknowledging close", _cts.Token).ConfigureAwait(false);
                    _ws.Dispose();
                    _ws = new();
                }
            }
#pragma warning disable CA1031 // re-throwing in current context silently breaks the background loop
            catch (Exception ex)
#pragma warning restore CA1031
            {
                OnIgnoredException(_logger, ex, ex.ToString());
            }
        }
    }

    [LoggerMessage(EventId = 4, EventName = nameof(OnIgnoredException), Level = LogLevel.Error, Message = "Ignoring exception in background listener: {Message}")]
    static partial void OnIgnoredException(ILogger logger, Exception exception, string message);

    private async Task ProcessTextMessageAsync(SurrealTextRpcResponse response)
    {
        if (TryGetResponseIdFromJsonBuffer(response, out var id))
        {
            if (_responseHandlers.TryRemove(id, out var tcs))
            {
                try
                {
                    tcs.SetResult(response);
                }
#pragma warning disable CA1031 // Caught exception is set to TaskCompletionSource that is being await-ed, re-throwing the exception
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    tcs.SetException(ex);
                }
            }
            else
            {
                throw new SurrealException($"Received response for unknown id: {id}");
            }
        }
        else
        {
            var liveQueryNotification = JsonSerializer.Deserialize<SurrealNotification<SurrealLiveNotification>>(response.Buffer.Span);

            OnNotificationReceived(_logger, liveQueryNotification);

            var liveQueryId = new SurrealLiveQueryId(liveQueryNotification.Result.Id);

            await _liveQueryHandlers[liveQueryId].Invoke(liveQueryNotification.Result.Result, liveQueryNotification.Result.Action.ToUpperInvariant() switch
            {
                "CREATE" => SurrealEventType.Create,
                "UPDATE" => SurrealEventType.Update,
                "DELETE" => SurrealEventType.Delete,
            }).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 6, EventName = nameof(OnNotificationReceived), Level = LogLevel.Information, Message = "Notification: {Notification}")]
    static partial void OnNotificationReceived(ILogger logger, SurrealNotification<SurrealLiveNotification> notification);

    private readonly struct SurrealNotification<T>
    {
        [JsonPropertyName("result")]
        public required T Result { get; init; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    private readonly struct SurrealLiveNotification
    {
        [JsonPropertyName("action")]
        public required string Action { get; init; }

        [JsonPropertyName("id")]
        public required Guid Id { get; init; }

        [JsonPropertyName("result")]
        public required JsonElement Result { get; init; }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _optionsChangeToken?.Dispose();
        _cts.Dispose();
        _ws.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ReadUntilResult(ref Utf8JsonReader reader, int depth = 1)
    {
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName && reader.ValueTextEquals("result"u8) && reader.CurrentDepth == depth)
                return;
        }
    }

    private static T? ParseSingle<T>(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);

        ReadUntilResult(ref reader);

        return !reader.Read()
            ? throw new SurrealException("result was supposed to be a json object")
            : JsonSerializer.Deserialize<T>(ref reader);
    }

    private JsonWriterOptions _writerOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        SkipValidation = false,
    };

    private SurrealTextRpcRequest BuildJsonRequest(ReadOnlySpan<byte> methodUtf8)
    {
        var request = new SurrealTextRpcRequest
        {
            Buffer = new ArrayPoolBufferWriter<byte>(1024), // TODO: Make send buffer size configurable
            Options = _options,
        };

        using var writer = new Utf8JsonWriter(request.Buffer, _writerOptions);

        writer.WriteStartObject();
        writer.WriteString("id"u8, request.Id);
        writer.WriteString("method"u8, methodUtf8);
        writer.WriteEndObject();

        return request;
    }

    private SurrealTextRpcRequest BuildJsonRequest<TState>(ReadOnlySpan<byte> methodUtf8, TState state, Action<Utf8JsonWriter, TState> paramsWriter)
    {
        var request = new SurrealTextRpcRequest
        {
            Buffer = new ArrayPoolBufferWriter<byte>(1024), // TODO: Make send buffer size configurable
            Options = _options,
        };

        using var writer = new Utf8JsonWriter(request.Buffer, _writerOptions);

        writer.WriteStartObject();
        writer.WriteString("id"u8, request.Id);
        writer.WriteString("method"u8, methodUtf8);
        if (paramsWriter is not null)
        {
            writer.WriteStartArray("params"u8);
            paramsWriter(writer, state);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();

        return request;
    }
}
