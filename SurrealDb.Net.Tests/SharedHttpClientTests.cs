using System.Net;
using System.Net.Sockets;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Tests;

public class SharedHttpClientTests
{
    [Test]
    public async Task ShouldNotSendCookiesSetByAnEarlierResponse()
    {
        var tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var receivedCookies = new List<string?>();
        var serverTask = Task.Run(async () =>
        {
            for (int index = 0; index < 2; index++)
            {
                var context = await listener.GetContextAsync();
                receivedCookies.Add(context.Request.Headers["Cookie"]);
                context.Response.Headers.Add("Set-Cookie", "session=secret; Path=/");
                context.Response.Close();
            }
        });

        using var httpClient = SharedHttpClient.Create();
        (await httpClient.GetAsync($"http://127.0.0.1:{port}/export")).Dispose();
        (await httpClient.GetAsync($"http://127.0.0.1:{port}/import")).Dispose();

        await serverTask;

        receivedCookies.Should().Equal(null, null);
    }
}
