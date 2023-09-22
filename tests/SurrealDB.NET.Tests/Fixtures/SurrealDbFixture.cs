using SurrealDB.NET.Rpc;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using SurrealDB.NET.Http;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = false, MaxParallelThreads = -1)]

namespace SurrealDB.NET.Tests.Fixtures;

public sealed class SurrealDbCliFixture : IAsyncLifetime, IDisposable
{
	public int Port { get; } = AllocatePort();

	public ISurrealHttpClient Http { get; private set; }
	public ISurrealRpcClient Rpc { get; private set; }

	private readonly Process _process = new()
	{
		StartInfo = new ProcessStartInfo
		{
			FileName = "surreal",
			RedirectStandardOutput = false,
			RedirectStandardError = false,
			UseShellExecute = false,
			CreateNoWindow = true,
		},
		EnableRaisingEvents = true,
	};

	public Task DisposeAsync()
	{
		_process.Kill();
		Dispose();
		return Task.CompletedTask;
	}

	public async Task InitializeAsync()
	{
		_process.StartInfo.Arguments = $"start --auth -u root -p root -A --bind 0.0.0.0:{Port}";
		_process.Start();
		using var http = new HttpClient();
		http.BaseAddress = new Uri($"http://localhost:{Port}");
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var ok = false;
		while (ok is false)
		{
			using var response = await http.GetAsync("/health", cts.Token).ConfigureAwait(false);
			ok = response.IsSuccessStatusCode;
		}

		using var root = new SurrealTextRpcClient($"ws://localhost:{Port}", "test", "test");
		await root.SigninRootAsync("root", "root").ConfigureAwait(false);

		_ = await root.QueryAsync("""
            DEFINE TABLE user SCHEMAFULL;
            DEFINE FIELD email ON user TYPE string;
            DEFINE FIELD password ON user TYPE string;
            DEFINE INDEX index_user_email ON user COLUMNS email UNIQUE;
            DEFINE TABLE post SCHEMAFULL PERMISSIONS FULL;
            DEFINE FIELD content ON post TYPE string;
            DEFINE FIELD tags ON post TYPE option<string> DEFAULT NONE;
            """).ConfigureAwait(false);

		_ = await root.QueryAsync("""
            DEFINE SCOPE account SESSION 24h
                SIGNUP (CREATE user SET email = $email, password = crypto::argon2::generate($password))
                SIGNIN (SELECT * FROM user WHERE email = $email AND crypto::argon2::compare(password, $password))
            """).ConfigureAwait(false);

		_ = await root.QueryAsync("""
			USE NS test DB test;
			DEFINE USER test_root ON NAMESPACE PASSWORD "test_root";
			""");

		Http = new SurrealJsonHttpClient($"http://localhost:{Port}", "test", "test");
		Rpc = new SurrealTextRpcClient($"ws://localhost:{Port}", "test", "test");
	}

	private static int AllocatePort()
	{
		int freePort;
		using (TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0))
		{
			tcpListener.Start();
			freePort = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
			tcpListener.Stop();
		}
		return freePort;
	}

	public void Dispose()
	{
		_process.Dispose();
		Rpc?.Dispose();
	}
}

[CollectionDefinition(Name, DisableParallelization = false)]
public sealed class SurrealDbCollectionFixture
	: ICollectionFixture<SurrealDbCliFixture>
{
	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.

	public const string Name = "SurrealDB";
}
