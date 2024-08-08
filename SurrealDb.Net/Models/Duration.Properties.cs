namespace SurrealDb.Net.Models;

public readonly partial struct Duration
{
    /// <summary>
    /// A Zero duration (equals to "0ns")
    /// </summary>
    public static readonly Duration Zero = new();

    /// <summary>
    /// The <see cref="NanoSeconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int NanoSeconds { get; }

    /// <summary>
    /// The <see cref="MicroSeconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int MicroSeconds { get; }

    /// <summary>
    /// The <see cref="MilliSeconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int MilliSeconds { get; }

    /// <summary>
    /// The <see cref="Seconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Seconds { get; }

    /// <summary>
    /// The <see cref="Minutes"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Minutes { get; }

    /// <summary>
    /// The <see cref="Hours"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Hours { get; }

    /// <summary>
    /// The <see cref="Days"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Days { get; }

    /// <summary>
    /// The <see cref="Weeks"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Weeks { get; }

    /// <summary>
    /// The <see cref="Years"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Years { get; }
}
