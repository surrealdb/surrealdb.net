namespace SurrealDb.Net.Extensions.DependencyInjection;

public sealed class SurrealDbLoggingOptions
{
    /// <summary>
    /// Used to detect if application data should be included in logs.
    /// This typically include parameter values set for SURQL queries.
    /// You should only enable this flag if you have the appropriate security measures in place based on the sensitivity of this data.
    /// </summary>
    public bool SensitiveDataLoggingEnabled { get; set; }
}
