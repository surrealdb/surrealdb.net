namespace SurrealDb.Net.Attributes;

/// <summary>
/// Marks a relation property as the typed query navigation for the outgoing endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SurrealOutAttribute : Attribute;
