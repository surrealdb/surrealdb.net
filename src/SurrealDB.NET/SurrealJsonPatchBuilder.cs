using SurrealDB.NET.Json;
using System.Linq.Expressions;
using System.Text.Json;

namespace SurrealDB.NET;

public sealed class SurrealJsonPatchBuilder<T> : IDisposable
{
	private readonly Utf8JsonWriter _jsonWriter;
	private readonly JsonSerializerOptions _options;

	public SurrealJsonPatchBuilder(Utf8JsonWriter writer, JsonSerializerOptions options)
	{
		_jsonWriter = writer;
		_options = options;
	}

	private void WriteOperation<TValue>(ReadOnlySpan<byte> utf8op, Expression<Func<T, TValue>> pointer)
	{
		_jsonWriter.WriteStartObject();
		_jsonWriter.WriteString("op"u8, utf8op);
		_jsonWriter.WritePathFromExpression("path"u8, pointer, _options);
		_jsonWriter.WriteEndObject();
	}

	private void WriteOperation<TValue>(ReadOnlySpan<byte> utf8op, Expression<Func<T, TValue>> pointer, TValue value)
	{
		_jsonWriter.WriteStartObject();
		_jsonWriter.WriteString("op"u8, utf8op);
		_jsonWriter.WritePathFromExpression("path"u8, pointer, _options);
		_jsonWriter.WritePropertyName("value"u8);
		JsonSerializer.Serialize(_jsonWriter, value, _options);
		_jsonWriter.WriteEndObject();
	}

	private void WriteOperation<TValue>(ReadOnlySpan<byte> utf8op, Expression<Func<T, TValue>> from, Expression<Func<T, TValue>> to)
	{
		_jsonWriter.WriteStartObject();
		_jsonWriter.WriteString("op"u8, utf8op);
		_jsonWriter.WritePathFromExpression("from"u8, from, _options);
		_jsonWriter.WritePathFromExpression("path"u8, to, _options);
		_jsonWriter.WriteEndObject();
	}

	public SurrealJsonPatchBuilder<T> Add<TValue>(Expression<Func<T, TValue>> jsonPointer, TValue value)
	{
		WriteOperation("add"u8, jsonPointer, value);
		return this;
	}

	public SurrealJsonPatchBuilder<T> Remove<TValue>(Expression<Func<T, TValue>> jsonPointer)
	{
		WriteOperation("remove"u8, jsonPointer);
		return this;
	}

	public SurrealJsonPatchBuilder<T> Replace<TValue>(Expression<Func<T, TValue>> jsonPointer, TValue value)
	{
		WriteOperation("replace"u8, jsonPointer, value);
		return this;
	}

	public SurrealJsonPatchBuilder<T> Copy<TValue>(Expression<Func<T, TValue>> from, Expression<Func<T, TValue>> to)
	{
		WriteOperation("copy"u8, from, to);
		return this;
	}

	public SurrealJsonPatchBuilder<T> Move<TValue>(Expression<Func<T, TValue>> from, Expression<Func<T, TValue>> to)
	{
		WriteOperation("move"u8, from, to);
		return this;
	}

	public SurrealJsonPatchBuilder<T> Test<TValue>(Expression<Func<T, TValue>> jsonPointer, TValue value)
	{
		WriteOperation("test"u8, jsonPointer, value);
		return this;
	}

	public void Dispose()
	{
		_jsonWriter.Dispose();
	}
}
