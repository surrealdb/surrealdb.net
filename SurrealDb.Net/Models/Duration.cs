using SurrealDb.Net.Internals.Constants;

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
		if (_unitValues == null && other._unitValues == null)
			return true;

		if (_unitValues == null || other._unitValues == null)
			return false;

		if (_unitValues.Count != other._unitValues.Count)
			return false;

		foreach (var (key, value) in _unitValues)
		{
			if (!other._unitValues.TryGetValue(key, out var otherValue))
				return false;

			if (value != otherValue)
				return false;
		}

		return true;
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
		return HashCode.Combine(_value);
	}
	public override string ToString()
	{
		return _value ?? DurationConstants.DefaultDuration;
	}
}
