using SurrealDb.Embedded.Options;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class SurrealDbEmbeddedOptionsBuilder
{
    private readonly SurrealDbEmbeddedOptions _inner = new();

    public SurrealDbEmbeddedOptionsBuilder WithStrictMode(bool strictMode)
    {
        _inner.StrictMode = strictMode;
        return this;
    }

    public SurrealDbEmbeddedOptionsBuilder WithExperimentalFeatures(bool enabled)
    {
        EnsuresAllowExperimentalCreated().Bool = true;
        EnsuresAllowExperimentalCreated().Array = null;
        return this;
    }

    public SurrealDbEmbeddedOptionsBuilder WithExperimentalFeatures(IEnumerable<string> targets)
    {
        EnsuresAllowExperimentalCreated().Bool = null;
        EnsuresAllowExperimentalCreated().Array = targets;
        return this;
    }

    public SurrealDbEmbeddedOptionsBuilder WithoutExperimentalFeatures(bool enabled)
    {
        EnsuresDenyExperimentalCreated().Bool = true;
        EnsuresDenyExperimentalCreated().Array = null;
        return this;
    }

    public SurrealDbEmbeddedOptionsBuilder WithoutExperimentalFeatures(IEnumerable<string> targets)
    {
        EnsuresDenyExperimentalCreated().Bool = null;
        EnsuresDenyExperimentalCreated().Array = targets;
        return this;
    }

    private SurrealDbEmbeddedTargetsConfig EnsuresAllowExperimentalCreated()
    {
        var experimental = EnsuresExperimentalCreated();

        if (experimental.Allow is not null)
            return experimental.Allow;

        var allow = new SurrealDbEmbeddedTargetsConfig();
        experimental.Allow = allow;

        return allow;
    }

    private SurrealDbEmbeddedTargetsConfig EnsuresDenyExperimentalCreated()
    {
        var experimental = EnsuresExperimentalCreated();

        if (experimental.Deny is not null)
            return experimental.Deny;

        var deny = new SurrealDbEmbeddedTargetsConfig();
        experimental.Deny = deny;

        return deny;
    }

    private SurrealDbEmbeddedTargets EnsuresExperimentalCreated()
    {
        var capabilities = EnsuresCapabilitiesCreated();

        if (capabilities.Experimental is not null)
            return capabilities.Experimental;

        var experimental = new SurrealDbEmbeddedTargets();
        capabilities.Experimental = experimental;

        return experimental;
    }

    private SurrealDbEmbeddedCapabilities EnsuresCapabilitiesCreated()
    {
        if (_inner.Capabilities is not null)
            return _inner.Capabilities;

        var capabilities = new SurrealDbEmbeddedCapabilities();
        _inner.Capabilities = capabilities;

        return capabilities;
    }

    public SurrealDbEmbeddedOptions Build()
    {
        return _inner;
    }
}
