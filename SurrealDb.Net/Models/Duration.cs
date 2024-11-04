using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Models;

/// <summary>
/// Represents a SurrealDB duration.<br />
/// Its sole purpose is to be more precise than <see cref="TimeSpan"/>
/// and to be able to read the nanosecond part of the <see cref="Duration"/> data type.
/// In fact, <see cref="TimeSpan"/> maximum precision is <see cref="TimeSpan.Ticks"/>.
/// The <see cref="TimeSpan.MinValue"/> of a <see cref="TimeSpan"/> is 1 <see cref="TimeSpan.Ticks"/> which represent 100 nanoseconds.
/// If a precision under 100 nanoseconds is required, then use <see cref="Duration"/>.<br /><br />
///
/// Otherwise, you may simply want to use <see cref="TimeSpan"/>.
/// </summary>
public readonly partial struct Duration : IEquatable<Duration>, IComparable<Duration>
{
    public bool Equals(Duration other)
    {
        return NanoSeconds == other.NanoSeconds
            && MicroSeconds == other.MicroSeconds
            && MilliSeconds == other.MilliSeconds
            && Seconds == other.Seconds
            && Minutes == other.Minutes
            && Hours == other.Hours
            && Days == other.Days
            && Weeks == other.Weeks
            && Years == other.Years;
    }

    public int CompareTo(Duration other)
    {
        if (Years != other.Years)
            return Years.CompareTo(other.Years);

        if (Weeks != other.Weeks)
            return Weeks.CompareTo(other.Weeks);

        if (Days != other.Days)
            return Days.CompareTo(other.Days);

        if (Hours != other.Hours)
            return Hours.CompareTo(other.Hours);

        if (Minutes != other.Minutes)
            return Minutes.CompareTo(other.Minutes);

        if (Seconds != other.Seconds)
            return Seconds.CompareTo(other.Seconds);

        if (MilliSeconds != other.MilliSeconds)
            return MilliSeconds.CompareTo(other.MilliSeconds);

        if (MicroSeconds != other.MicroSeconds)
            return MicroSeconds.CompareTo(other.MicroSeconds);

        if (NanoSeconds != other.NanoSeconds)
            return NanoSeconds.CompareTo(other.NanoSeconds);

        return 0;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Duration other)
            return Equals(other);

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            NanoSeconds
                + MicroSeconds * TimeConstants.NANOS_PER_MICROSECOND
                + MilliSeconds * TimeConstants.NANOS_PER_MILLISECOND,
            Seconds,
            Minutes,
            Hours,
            Days,
            Weeks,
            Years
        );
    }

    public override string ToString()
    {
        if (this == Zero)
        {
            const string DEFAULT_DURATION_STRING = "0ns";
            return DEFAULT_DURATION_STRING;
        }

        return string.Create(
            CalculateStringLength(),
            this,
            (buffer, self) =>
            {
                if (self.Years != 0)
                {
                    buffer.Write(self.Years);
                    buffer.Write('y');
                }
                if (self.Weeks != 0)
                {
                    buffer.Write(self.Weeks);
                    buffer.Write('y');
                }
                if (self.Days != 0)
                {
                    buffer.Write(self.Days);
                    buffer.Write('d');
                }
                if (self.Hours != 0)
                {
                    buffer.Write(self.Hours);
                    buffer.Write('h');
                }
                if (self.Minutes != 0)
                {
                    buffer.Write(self.Minutes);
                    buffer.Write('m');
                }
                if (self.Seconds != 0)
                {
                    buffer.Write(self.Seconds);
                    buffer.Write('s');
                }
                if (self.MilliSeconds != 0)
                {
                    buffer.Write(self.MilliSeconds);
                    buffer.Write("ms");
                }
                if (self.MicroSeconds != 0)
                {
                    buffer.Write(self.MicroSeconds);
                    buffer.Write("us");
                }
                if (self.NanoSeconds != 0)
                {
                    buffer.Write(self.NanoSeconds);
                    buffer.Write("ns");
                }
            }
        );
    }

    private int CalculateStringLength()
    {
        int total = 0;

        if (Years != 0)
            total += CalculateInt32Length(Years) + 1;
        if (Weeks != 0)
            total += CalculateInt32Length(Weeks) + 1;
        if (Days != 0)
            total += CalculateInt32Length(Days) + 1;
        if (Hours != 0)
            total += CalculateInt32Length(Hours) + 1;
        if (Minutes != 0)
            total += CalculateInt32Length(Minutes) + 1;
        if (Seconds != 0)
            total += CalculateInt32Length(Seconds) + 1;
        if (MilliSeconds != 0)
            total += CalculateInt32Length(MilliSeconds) + 2;
        if (MicroSeconds != 0)
            total += CalculateInt32Length(MicroSeconds) + 2;
        if (NanoSeconds != 0)
            total += CalculateInt32Length(NanoSeconds) + 2;

        return total;
    }

    private static int CalculateInt32Length(int i)
    {
        if (i == 0)
        {
            return 1;
        }

        if (i < 0)
        {
            return CalculateInt32Length(Math.Abs(i)) + 1;
        }

        return (int)Math.Floor(Math.Log10(i)) + 1;
    }
}
