using SurrealDb.Net.LocalBenchmarks.Models;

namespace SurrealDb.Net.LocalBenchmarks.Parsers;

internal static class FromSpanDurationParser
{
    public static Dictionary<DurationUnit, int> Parse(ReadOnlySpan<char> chars)
    {
        var result = new Dictionary<DurationUnit, int>();

        Span<byte> valueBuffer =
            chars.Length <= 128 ? stackalloc byte[chars.Length - 1] : new byte[chars.Length - 1];
        Span<byte> unitBuffer = stackalloc byte[2];

        int valueIndex = 0;
        int unitIndex = 0;

        bool lastIsUnit = false;

        for (int index = 0; index < chars.Length; index++)
        {
            char c = chars[index];

            if (char.IsDigit(c))
            {
                if (lastIsUnit)
                {
                    if (valueIndex > 0)
                    {
                        var value = int.Parse(valueBuffer[..valueIndex]);
                        var unit = ExtractDurationUnit(unitBuffer, unitIndex);

                        result.Add(unit, value);
                    }

                    lastIsUnit = false;
                    valueIndex = 0;

                    valueBuffer[valueIndex++] = (byte)c;
                }
                else
                {
                    valueBuffer[valueIndex++] = (byte)c;
                }
            }
            else
            {
                if (index == 0)
                {
                    throw new Exception("Expected integer value");
                }

                if (lastIsUnit)
                {
                    unitBuffer[unitIndex++] = (byte)c;
                }
                else
                {
                    lastIsUnit = true;
                    unitIndex = 0;
                    unitBuffer[unitIndex++] = (byte)c;
                }
            }
        }

        if (valueIndex > 0)
        {
            var value = int.Parse(valueBuffer[..valueIndex]);
            var unit = ExtractDurationUnit(unitBuffer, unitIndex);

            result.Add(unit, value);
        }

        return result;
    }

    private static DurationUnit ExtractDurationUnit(ReadOnlySpan<byte> unitBuffer, int unitIndex)
    {
        return unitIndex switch
        {
            1
                => unitBuffer[0] switch
                {
                    (byte)'y' => DurationUnit.Year,
                    (byte)'w' => DurationUnit.Week,
                    (byte)'d' => DurationUnit.Day,
                    (byte)'h' => DurationUnit.Hour,
                    (byte)'m' => DurationUnit.Minute,
                    (byte)'s' => DurationUnit.Second,
                    _ => throw new Exception("Invalid unit")
                },
            2 when unitBuffer[1] == (byte)'s'
                => unitBuffer[0] switch
                {
                    (byte)'m' => DurationUnit.MilliSecond,
                    (byte)'u' or (byte)'µ' => DurationUnit.MicroSecond,
                    (byte)'n' => DurationUnit.NanoSecond,
                    _ => throw new Exception("Invalid unit")
                },
            _ => throw new Exception("Invalid unit")
        };
    }
}
