using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

public class DependencyInjectionTests
{
    [Test]
    public async Task AddAgentMemoryRegistersAClientThatSendsThroughTheFactoryClient()
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var services = new ServiceCollection();
        services
            .AddAgentMemory(options =>
            {
                options.Context = "acme";
                options.ApiKey = "k";
                options.Endpoint = "https://api.test/";
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<AgentMemoryClient>();
        await client.WhoamiAsync();

        client.ContextId.Should().Be("acme");
        handler.Last.UrlText.Should().Be("https://api.test/api/v1/acme/me");
        handler.Last.Header("Authorization").Should().Be("Bearer k");
    }

    [Test]
    public async Task AddKeyedAgentMemoryRegistersOneClientPerKey()
    {
        var handler = FakeHandler.Json(Fixtures.Whoami);
        var services = new ServiceCollection();
        services
            .AddKeyedAgentMemory(
                "prod",
                options =>
                {
                    options.Context = "prod-context";
                    options.ApiKey = "prod-key";
                    options.Endpoint = "https://prod.test";
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services
            .AddKeyedAgentMemory(
                "staging",
                options =>
                {
                    options.Context = "staging-context";
                    options.ApiKey = "staging-key";
                    options.Endpoint = "https://staging.test";
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredKeyedService<AgentMemoryClient>("prod").WhoamiAsync();
        await provider.GetRequiredKeyedService<AgentMemoryClient>("staging").WhoamiAsync();

        handler.Requests[0].UrlText.Should().Be("https://prod.test/api/v1/prod-context/me");
        handler.Requests[0].Header("Authorization").Should().Be("Bearer prod-key");
        handler.Requests[1].UrlText.Should().Be("https://staging.test/api/v1/staging-context/me");
        handler.Requests[1].Header("Authorization").Should().Be("Bearer staging-key");
    }

    [Test]
    public void ValidatesTheOptionsAtRegistration()
    {
        var services = new ServiceCollection();

        var act = () =>
            services.AddAgentMemory(options =>
            {
                options.Context = "acme";
                options.ApiKey = "k";
                options.Endpoint = "not a url";
            });

        act.Should().Throw<ArgumentException>().WithMessage("*endpoint*");
    }

    [Test]
    public async Task UsesAnInfiniteHttpClientTimeoutSoUploadsAreNotCutOff()
    {
        var services = new ServiceCollection();
        services.AddAgentMemory(options =>
        {
            options.Context = "acme";
            options.ApiKey = "k";
            options.Endpoint = "https://api.test";
        });
        await using var provider = services.BuildServiceProvider();

        var httpClient = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("SurrealDb.AgentMemory");

        httpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Test]
    public async Task DoesNotRetainClientsResolvedFromTheRootProvider()
    {
        // The client holds nothing to dispose, so the container has no reason to keep the instances it hands out.
        var services = new ServiceCollection();
        services.AddAgentMemory(options =>
        {
            options.Context = "acme";
            options.ApiKey = "k";
            options.Endpoint = "https://api.test";
        });
        await using var provider = services.BuildServiceProvider();

        var clients = ResolveMany(provider, 1_000);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        typeof(AgentMemoryClient).Should().NotImplement<IDisposable>();
        clients.Count(client => client.TryGetTarget(out _)).Should().Be(0);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    private static List<WeakReference<AgentMemoryClient>> ResolveMany(
        IServiceProvider provider,
        int count
    )
    {
        var clients = new List<WeakReference<AgentMemoryClient>>(count);
        for (var i = 0; i < count; i++)
        {
            clients.Add(new(provider.GetRequiredService<AgentMemoryClient>()));
        }
        return clients;
    }

    [Test]
    public void ValidatesTheRetryCountAndApiKeyAtRegistration()
    {
        var services = new ServiceCollection();

        var negativeRetries = () =>
            services.AddAgentMemory(options =>
            {
                options.Context = "acme";
                options.ApiKey = "k";
                options.Endpoint = "https://api.test";
                options.MaxRetries = -1;
            });
        var keyWithNewline = () =>
            services.AddAgentMemory(options =>
            {
                options.Context = "acme";
                options.ApiKey = "sp-key:1-secret\n";
                options.Endpoint = "https://api.test";
            });

        negativeRetries.Should().Throw<ArgumentOutOfRangeException>();
        keyWithNewline.Should().Throw<ArgumentException>().WithMessage("*trailing newline*");
    }

    [Test]
    public void ValidatesTheTimeoutContextAndEndpointAtRegistration()
    {
        static Action Register(Action<AgentMemoryOptions> change)
        {
            return () =>
                new ServiceCollection().AddAgentMemory(options =>
                {
                    options.Context = "acme";
                    options.ApiKey = "k";
                    options.Endpoint = "https://api.test";
                    change(options);
                });
        }

        Register(options => options.Timeout = TimeSpan.Zero)
            .Should()
            .Throw<ArgumentOutOfRangeException>();
        Register(options => options.Context = "..").Should().Throw<ArgumentException>();
        Register(options => options.Endpoint = "https://api.test\n")
            .Should()
            .Throw<ArgumentException>();
        Register(options => options.ApiKey = "sp-k\u00e9y-1").Should().Throw<ArgumentException>();
        Register(_ => { }).Should().NotThrow();
    }
}
