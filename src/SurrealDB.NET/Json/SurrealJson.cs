using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET.Json;

public static class SurrealJson
{
	internal static JsonElement BytesToJsonElement(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        return JsonElement.ParseValue(ref reader);
    }

	internal static JsonElement BytesToJsonElement(Stream json)
	{
		Span<byte> buffer = stackalloc byte[(int)json.Length];
		json.Read(buffer);
		var reader = new Utf8JsonReader(buffer);
		return JsonElement.ParseValue(ref reader);
	}

	internal static void WritePathFromExpression<T, TProperty>(this Utf8JsonWriter writer, ReadOnlySpan<byte> utf8key, Expression<Func<T, TProperty>> expression, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[512];  // A stack-allocated buffer; adjust size as needed
		int offset = buffer.Length;

		Expression? body = expression.Body;
		while (body is MemberExpression memberExpression)
		{
			if (memberExpression.Member is PropertyInfo propertyInfo)
			{
				string propertyName = propertyInfo.Name;

				var jsonPropertyNameAttribute = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
				if (jsonPropertyNameAttribute != null)
				{
					propertyName = jsonPropertyNameAttribute.Name;
				}
				else if (options.PropertyNamingPolicy != null)
				{
					propertyName = options.PropertyNamingPolicy.ConvertName(propertyName);
				}

				Span<byte> utf8PropertyName = stackalloc byte[Encoding.UTF8.GetByteCount(propertyName)];
				Encoding.UTF8.GetBytes(propertyName.AsSpan(), utf8PropertyName);

				utf8PropertyName.CopyTo(buffer.Slice(offset - utf8PropertyName.Length, utf8PropertyName.Length));
				offset -= utf8PropertyName.Length;

				buffer[--offset] = (byte)'/';
			}

			body = memberExpression.Expression;
		}

		writer.WriteString(utf8key, buffer.Slice(offset, buffer.Length - offset));
	}

	public static void ThrowOnError(JsonElement element)
	{
		if (element.ValueKind is JsonValueKind.Array)
		{
			var errors = element
				.EnumerateArray()
				.Where(r => r.TryGetProperty("status"u8, out var status) && status.GetString() is "ERR");

			if (errors.Any())
				throw new AggregateException("Multiple surrealdb errors occured", errors
					.Select(e => e.GetProperty("result"u8).GetString()).Select(m => new SurrealException(m ?? "SurrealDB server responded with error but no message")));
		}
		else if (element.ValueKind is JsonValueKind.Object)
		{
			if (element.TryGetProperty("error"u8, out var error))
			{
				if (error.ValueKind is JsonValueKind.String)
					throw new SurrealException(error.GetString()!);
				else if (error.ValueKind is JsonValueKind.Object && error.TryGetProperty("message"u8, out var message))
					throw new SurrealException(message.GetString()!);
			}
			if (element.TryGetProperty("status"u8, out var status) && status.GetString() is "ERR")
			{
				throw new SurrealException(element.GetProperty("result"u8).GetString()!);
			}
		}
	}
}
