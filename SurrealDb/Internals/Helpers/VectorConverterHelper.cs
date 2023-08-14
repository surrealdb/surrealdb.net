using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace SurrealDb.Internals.Helpers;

internal static class VectorConverterHelper
{
	public static bool TryReadVectorFromJsonArray(ref Utf8JsonReader reader, byte length, out float[] values)
	{
		values = new float[length];

		for (byte index = 0; index < length; index++)
		{
			if (!reader.Read())
				return false;

			values[index] = reader.TokenType switch
			{
				JsonTokenType.Number => reader.GetSingle(),
				JsonTokenType.String => TryParseFloat(reader.GetString()!, out var value)
					? value
					: throw new JsonException($"Cannot deserialize {nameof(Vector)}{length}"),
				_ => throw new JsonException($"Cannot deserialize {nameof(Vector)}{length}")
			};
		}

		return true;
	}

	public static bool TryParseFloat(string input, out float result)
	{
		return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}
}
