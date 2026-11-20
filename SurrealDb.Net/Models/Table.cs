namespace SurrealDb.Net.Models;

/// <summary>
/// The name of a table, sent to and received from SurrealDB as a table value rather than a string.
/// </summary>
/// <example>
/// <code>
/// await client.RawQuery("SELECT * FROM $tb", new Dictionary&lt;string, object?&gt; { { "tb", new Table("person") } });
/// </code>
/// </example>
public readonly struct Table : IEquatable<Table>, IComparable<Table>
{
    /// <summary>
    /// The name of the table, e.g. <c>person</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Instanciate a new <see cref="Table"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">A name is required</exception>
    public Table()
    {
        throw new InvalidOperationException("A name is required");
    }

    /// <summary>
    /// Instanciate a new <see cref="Table"/>.
    /// </summary>
    /// <param name="name">The name of the table, e.g. <c>person</c>.</param>
    /// <exception cref="ArgumentException">The name is null or empty</exception>
    public Table(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("A table name is required.", nameof(name));
        }

        Name = name;
    }

    public override bool Equals(object? obj)
    {
        return obj is Table table && Equals(table);
    }

    public override int GetHashCode()
    {
        return Name?.GetHashCode() ?? 0;
    }

    public bool Equals(Table other)
    {
        return string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public int CompareTo(Table other)
    {
        return string.CompareOrdinal(Name, other.Name);
    }

    public override string ToString()
    {
        return Name;
    }

    public static bool operator ==(Table left, Table right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Table left, Table right)
    {
        return !(left == right);
    }

    public static explicit operator Table(string name)
    {
        return new(name);
    }
}
