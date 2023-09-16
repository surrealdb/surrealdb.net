using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET.Json;

public static class SurrealJson
{
    public static JsonElement BytesToJsonElement(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
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
}
