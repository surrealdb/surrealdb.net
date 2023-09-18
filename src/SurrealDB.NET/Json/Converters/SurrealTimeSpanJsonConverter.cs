using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET.Json.Converters;

public sealed class SurrealTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new InvalidOperationException($"Can not deserialize JSON {reader.TokenType} to TimeSpan");

		return ParseDuration(reader.ValueSpan);
	}

	public static TimeSpan ParseDuration(ReadOnlySpan<byte> input)
	{
		long totalTicks = 0;
		double value = 0;
		double numberBase = 1;

		const byte y = 0x79;
		const byte w = 0x77;
		const byte d = 0x64;
		const byte h = 0x68;
		const byte m = 0x6d;
		const byte s = 0x73;
		const byte mu1 = 0xc2;
		const byte mu2 = 0xb5;
		const byte zero = 0x30;
		const byte one = 0x31;
		const byte nine = 0x39;
		const byte dot = 0x2e;

		for (var i = 0; i < input.Length; i++)
		{
			if (input[i] is zero)
			{
				numberBase *= numberBase < 1 ? 0.1 : 1;
				continue;
			}

			if (input[i] is >= one and <= nine)
			{
				value = value * 10 + (input[i] - zero) * numberBase;
				numberBase *= numberBase < 1 ? 0.1 : 1;
				continue;
			}

			if (input[i] is dot)
			{
				numberBase = 0.1;
				continue;
			}

			var (increment, skip) = input[i] switch
			{
				y => (value * TimeSpan.TicksPerDay * 365L, 0),
				w => (value * TimeSpan.TicksPerDay * 7L, 0),
				d => (value * TimeSpan.TicksPerDay, 0),
				h => (value * TimeSpan.TicksPerHour, 0),
				m when i + 1 < input.Length && input[i + 1] is s => (value * TimeSpan.TicksPerMillisecond, 1),
				m => (value * TimeSpan.TicksPerMinute, 0),
				s => (value * TimeSpan.TicksPerSecond, 0),
				mu1 when i + 2 < input.Length && input[i + 1] is mu2 && input[i + 2] is s => (value * TimeSpan.TicksPerMicrosecond, 2),
				_ => throw new InvalidOperationException("Can not deserialize JSON string to TimeSpan: invalid format"),
			};

			totalTicks += (long)increment;
			i += skip;
			value = 0;
			numberBase = 1;
		}

		return new TimeSpan(totalTicks);
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		throw new NotImplementedException("Not writing TimeSpan to json as surreal::duration; doing this later :)");
	}
}
