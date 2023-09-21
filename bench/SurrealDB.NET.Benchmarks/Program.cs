using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SurrealDB.NET;
using SurrealDB.NET.Geographic;
using SurrealDB.NET.Rpc;
using System.Text.Json.Serialization;

#pragma warning disable CA1822, CA1050

BenchmarkRunner.Run<Runner>();

[MemoryDiagnoser]
public class Runner
{
	private readonly ISurrealRpcClient _client;

	public Runner()
	{
		_client = new SurrealTextRpcClient("ws://localhost:8008", "demo", "demo");
	}

	[GlobalSetup]
	public async Task Initialize()
	{
		var token = await _client.SigninNamespaceAsync("demo", "demoroot", "demoroot").ConfigureAwait(false);
		await _client.AuthenticateAsync(token).ConfigureAwait(false);
	}

	[Benchmark]
	public async Task<Artist?> GetArtistOnly()
	{
		var result = await _client.QueryAsync("SELECT * FROM ONLY artist WHERE last_name = $last_name", new
		{
			last_name = "Spears"

		}).ConfigureAwait(false);

		return result.Get<Artist>();
	}

	[Benchmark]
	public async Task<Artist?> GetArtistNotFound()
	{
		var result = await _client.QueryAsync("SELECT * FROM type::thing($id)", new
		{
			id = Thing.Parse("artist:doesnotexist")

		}).ConfigureAwait(false);

		return result.Get<Artist?>();
	}
}

public sealed record Artist() : SurrealRecord("artist")
{
	[JsonPropertyName("company_name")]
	public string? CompanyName { get; init; }

	public string? Email { get; init; }

	[JsonPropertyName("first_name")]
	public string? FirstName { get; init; }

	[JsonPropertyName("last_name")]
	public string? LastName { get; init; }

	public string? Phone { get; init; }

	public Address? Address { get; init; }
}

public sealed record Address
{
	public string? AddressLine1 { get; init; }
	public string? AddressLine2 { get; init; }
	public string? City { get; init; }
	public string? Country { get; init; }
	[JsonPropertyName("post_code")]
	public string? PostCode { get; init; }
	public Point? Coordinates { get; init; }
}
