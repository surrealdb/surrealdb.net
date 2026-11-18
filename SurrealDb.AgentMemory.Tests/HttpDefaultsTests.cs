using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// The HTTP handler the package sets up when the caller brings none.
/// </summary>
public class HttpDefaultsTests
{
    private static AgentMemoryOptions Options(string key)
    {
        return new AgentMemoryOptions
        {
            Context = "c",
            ApiKey = key,
            Endpoint = "https://api.test",
        };
    }

    [Test]
    public void TheHandlerKeepsNoCookiesAndFollowsNoRedirects()
    {
        // It can serve clients that hold different API keys, so a cookie set in answer to one must not
        // ride along with another; and a redirect drops the Authorization header.
        using var handler = AgentMemoryHttp.CreateHandler();

        var sockets = handler.Should().BeOfType<SocketsHttpHandler>().Subject;
        sockets.UseCookies.Should().BeFalse();
        sockets.AllowAutoRedirect.Should().BeFalse();
        sockets.PooledConnectionLifetime.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Test]
    public void ClientsCreatedWithoutAnHttpClientShareOneConnectionPool()
    {
        var first = new AgentMemoryClient(Options("key-a"));
        var second = new AgentMemoryClient(Options("key-b"));

        first.Transport.HttpClient.Should().BeSameAs(second.Transport.HttpClient);
        first.Transport.HttpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Test]
    public async Task ADependencyInjectionClientGetsTheSameHandlerSettings()
    {
        var services = new ServiceCollection();
        services.AddAgentMemory(options =>
        {
            options.Context = "c";
            options.ApiKey = "k";
            options.Endpoint = "https://api.test";
        });
        await using var provider = services.BuildServiceProvider();

        HttpMessageHandler handler = provider
            .GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler("SurrealDb.AgentMemory");
        while (handler is DelegatingHandler { InnerHandler: { } inner })
        {
            handler = inner;
        }

        var sockets = handler.Should().BeOfType<SocketsHttpHandler>().Subject;
        sockets.UseCookies.Should().BeFalse();
        sockets.AllowAutoRedirect.Should().BeFalse();
    }

    [Test]
    public async Task ARedirectIsReportedWithItsTargetRatherThanFollowed()
    {
        var handler = new FakeHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect)
            {
                Content = new ByteArrayContent([]),
            };
            response.Headers.Location = new Uri("https://memory.example.com/api/v1/c/me");
            return response;
        });

        var act = () => TestClient.Create(handler).WhoamiAsync();

        var error = await act.Should().ThrowExactlyAsync<AgentMemoryException>();
        error.Which.Status.Should().Be(307);
        error.Which.Detail.Should().Contain("https://memory.example.com/api/v1/c/me");
        handler.Requests.Should().HaveCount(1);
    }
}
