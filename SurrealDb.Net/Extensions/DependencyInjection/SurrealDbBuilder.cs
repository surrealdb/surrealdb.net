namespace Microsoft.Extensions.DependencyInjection;

public sealed class SurrealDbBuilder
{
    public IServiceCollection Services { get; }

    public IServiceCollection And => Services;

    public SurrealDbBuilder(IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        Services = services;
    }
}
