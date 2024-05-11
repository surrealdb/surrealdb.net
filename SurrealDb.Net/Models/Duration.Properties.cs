using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Models;

// TODO : Avoid use of Dictionary<T>
// TODO : Is value still necessary for CBOR?

public readonly partial struct Duration
{
    private readonly string? _value;
    private readonly Dictionary<DurationUnit, int>? _unitValues = new();

    /// <summary>
    /// A Zero duration (equals to "0ns")
    /// </summary>
    public static readonly Duration Zero = new();

    /// <summary>
    /// The <see cref="NanoSeconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int NanoSeconds => _unitValues?.GetValueOrDefault(DurationUnit.NanoSecond, 0) ?? 0;

    /// <summary>
    /// The <see cref="MicroSeconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int MicroSeconds => _unitValues?.GetValueOrDefault(DurationUnit.MicroSecond, 0) ?? 0;

    /// <summary>
    /// The <see cref="MilliSeconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int MilliSeconds => _unitValues?.GetValueOrDefault(DurationUnit.MilliSecond, 0) ?? 0;

    /// <summary>
    /// The <see cref="Seconds"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Seconds => _unitValues?.GetValueOrDefault(DurationUnit.Second, 0) ?? 0;

    /// <summary>
    /// The <see cref="Minutes"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Minutes => _unitValues?.GetValueOrDefault(DurationUnit.Minute, 0) ?? 0;

    /// <summary>
    /// The <see cref="Hours"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Hours => _unitValues?.GetValueOrDefault(DurationUnit.Hour, 0) ?? 0;

    /// <summary>
    /// The <see cref="Days"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Days => _unitValues?.GetValueOrDefault(DurationUnit.Day, 0) ?? 0;

    /// <summary>
    /// The <see cref="Weeks"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Weeks => _unitValues?.GetValueOrDefault(DurationUnit.Week, 0) ?? 0;

    /// <summary>
    /// The <see cref="Years"/> part of the <see cref="Duration"/> type.
    /// </summary>
    public int Years => _unitValues?.GetValueOrDefault(DurationUnit.Year, 0) ?? 0;
}
