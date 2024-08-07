using System.Globalization;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Models;

public readonly partial struct Duration
{
    /// <summary>
    /// Creates a default <see cref="Duration"/>, equivalent to "0ns".
    /// </summary>
    public Duration() { }

    internal Duration(string input)
    {
        // 💡 Contains the current unit value, by iterating over the "input" (cannot exceed input length minus at least one unit char)
        Span<byte> valueBuffer =
            input.Length <= 128 ? stackalloc byte[input.Length - 1] : new byte[input.Length - 1];

        // 💡 Contains the current unit alongside the value, by iterating over the "input" (cannot exceed length of 2)
        Span<byte> unitBuffer = stackalloc byte[2];

        int valueIndex = 0;
        int unitIndex = 0;

        bool isLastCharUnit = false;

        int index = 0;

        while (true)
        {
            bool isBeyondInput = index >= input.Length;

            if (isBeyondInput || char.IsDigit(input[index]))
            {
                if (isBeyondInput || isLastCharUnit)
                {
                    if (isBeyondInput || valueIndex > 0)
                    {
                        Span<byte> currentValueSpan = valueBuffer[..valueIndex];
#if NET8_0_OR_GREATER
                        int value = int.Parse(currentValueSpan, CultureInfo.InvariantCulture);
#else
                        int value = int.Parse(
                            currentValueSpan.ToString(),
                            CultureInfo.InvariantCulture
                        );
#endif

                        if (unitIndex == 1)
                        {
                            switch (unitBuffer[0])
                            {
                                case (byte)'y':
                                    Years = value;
                                    break;
                                case (byte)'w':
                                    Weeks = value;
                                    break;
                                case (byte)'d':
                                    Days = value;
                                    break;
                                case (byte)'h':
                                    Hours = value;
                                    break;
                                case (byte)'m':
                                    Minutes = value;
                                    break;
                                case (byte)'s':
                                    Seconds = value;
                                    break;
                                default:
                                    throw new Exception("Invalid unit");
                            }
                        }
                        else if (unitIndex == 2 && unitBuffer[1] == (byte)'s')
                        {
                            switch (unitBuffer[0])
                            {
                                case (byte)'m':
                                    MilliSeconds = value;
                                    break;
                                case (byte)'u':
                                case (byte)'µ':
                                    MicroSeconds = value;
                                    break;
                                case (byte)'n':
                                    NanoSeconds = value;
                                    break;
                                default:
                                    throw new Exception("Invalid unit");
                            }
                        }
                        else
                        {
                            throw new Exception("Invalid unit");
                        }
                    }

                    // 💡 Stop reading if we exceed input length
                    if (isBeyondInput)
                        break;

                    // 💡 Read the next unit value, the first numeric char after a non-numeric char
                    isLastCharUnit = false;
                    valueIndex = 0;

                    valueBuffer[valueIndex++] = (byte)input[index];
                }
                else
                {
                    // 💡 Equivalent to concat current value (keep reading)
                    valueBuffer[valueIndex++] = (byte)input[index];
                }
            }
            else
            {
                // 💡 Start of input must be a numeric value
                if (index == 0)
                {
                    throw new Exception("Expected integer value");
                }

                if (isLastCharUnit)
                {
                    // 💡 Equivalent to concat current unit value (keep reading)
                    unitBuffer[unitIndex++] = (byte)input[index];
                }
                else
                {
                    // 💡 The first char after the numeric value (start reading current unit value)
                    isLastCharUnit = true;
                    unitIndex = 0;
                    unitBuffer[unitIndex++] = (byte)input[index];
                }
            }

            index++;
        }
    }

    /// <summary>
    /// Creates a <see cref="Duration"/> from <paramref name="seconds"/> and <paramref name="nanoseconds"/> parts.
    /// </summary>
    /// <param name="seconds">The total number of seconds to store in this <see cref="Duration"/>. Defaults to 0.</param>
    /// <param name="nanoseconds">The total number of nanoseconds to store in this <see cref="Duration"/>. Defaults to 0.</param>
    public Duration(long seconds = 0, int nanoseconds = 0)
    {
        if (seconds != 0)
        {
            long remainingSeconds = seconds;

            Years = (int)(remainingSeconds / TimeConstants.SECONDS_PER_YEAR);
            if (Years != 0)
            {
                remainingSeconds -= Years * TimeConstants.SECONDS_PER_YEAR;
            }

            Weeks = (int)(remainingSeconds / TimeConstants.SECONDS_PER_WEEK);
            if (Weeks != 0)
            {
                remainingSeconds -= Weeks * TimeConstants.SECONDS_PER_WEEK;
            }

            Days = (int)(remainingSeconds / TimeConstants.SECONDS_PER_DAY);
            if (Days != 0)
            {
                remainingSeconds -= Days * TimeConstants.SECONDS_PER_DAY;
            }

            Hours = (int)(remainingSeconds / TimeConstants.SECONDS_PER_HOUR);
            if (Hours != 0)
            {
                remainingSeconds -= Hours * TimeConstants.SECONDS_PER_HOUR;
            }

            Minutes = (int)(remainingSeconds / TimeConstants.SECONDS_PER_MINUTE);
            if (Minutes != 0)
            {
                remainingSeconds -= Minutes * TimeConstants.SECONDS_PER_MINUTE;
            }

            if (remainingSeconds != 0)
            {
                Seconds = (int)remainingSeconds;
            }
        }

        if (nanoseconds != 0)
        {
            int remainingNanos = nanoseconds;

            MilliSeconds = remainingNanos / TimeConstants.NANOS_PER_MILLISECOND;
            if (MilliSeconds != 0)
            {
                remainingNanos -= MilliSeconds * TimeConstants.NANOS_PER_MILLISECOND;
            }

            MicroSeconds = remainingNanos / TimeConstants.NANOS_PER_MICROSECOND;
            if (MicroSeconds != 0)
            {
                remainingNanos -= MicroSeconds * TimeConstants.NANOS_PER_MICROSECOND;
            }

            if (remainingNanos != 0)
            {
                NanoSeconds = remainingNanos;
            }
        }
    }
}
