namespace SurrealDb.Net.Models;

public readonly partial struct Duration
{
    public static bool operator ==(Duration left, Duration right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Duration left, Duration right)
    {
        return !(left == right);
    }

    public static bool operator <(Duration left, Duration right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Duration left, Duration right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(Duration left, Duration right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(Duration left, Duration right)
    {
        return left.CompareTo(right) >= 0;
    }
}
