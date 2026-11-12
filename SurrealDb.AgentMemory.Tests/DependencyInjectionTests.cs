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
}
