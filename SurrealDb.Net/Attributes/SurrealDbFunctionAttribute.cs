namespace SurrealDb.Net.Attributes;

/// <summary>
/// Mark this method as a SurrealDB function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SurrealDbFunctionAttribute : Attribute
{
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether the SurrealDB function is built-in or user-defined otherwise.
    /// User-defined functions are prefixed with <c>fn::</c>.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    public SurrealDbFunctionAttribute(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return IsBuiltIn ? Name : $"fn::{Name}";
    }
}
