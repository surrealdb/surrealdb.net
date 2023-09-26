using BenchmarkDotNet.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks;

public class ScenarioBench : BaseBenchmark
{
	private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators = new SurrealDbClientGenerator[4];

	private ISurrealDbClient? _surrealdbHttpClient;
	private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;
	private ISurrealDbClient? _surrealdbWsTextClient;

	[GlobalSetup]
	public async Task GlobalSetup()
	{
		for (int index = 0; index < 4; index++)
		{
			var clientGenerator = new SurrealDbClientGenerator();
			var dbInfo = clientGenerator.GenerateDatabaseInfo();

			await CreateEcommerceTables(WsUrl, dbInfo);

			switch (index)
			{
				case 0:
					_surrealdbHttpClient = new SurrealDbClient(HttpUrl);
					InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
					await _surrealdbHttpClient.Connect();
					break;
				case 1:
					_surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(HttpUrl);
					InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
					await _surrealdbHttpClientWithHttpClientFactory.Connect();
					break;
				case 2:
					_surrealdbWsTextClient = new SurrealDbClient(WsUrl);
					InitializeSurrealDbClient(_surrealdbWsTextClient, dbInfo);
					await _surrealdbWsTextClient.Connect();
					break;
			}
		}
	}

	[GlobalCleanup]
	public async Task GlobalCleanup()
	{
		foreach (var clientGenerator in _surrealDbClientGenerators!)
		{
			if (clientGenerator is not null)
				await clientGenerator.DisposeAsync();
		}

		_surrealdbHttpClient?.Dispose();
		_surrealdbHttpClientWithHttpClientFactory?.Dispose();
		_surrealdbWsTextClient?.Dispose();
	}

	[Benchmark]
	public Task<List<ProductAlsoPurchased>> Http()
	{
		return Run(_surrealdbHttpClient!);
	}

	[Benchmark]
	public Task<List<ProductAlsoPurchased>> HttpWithClientFactory()
	{
		return Run(_surrealdbHttpClientWithHttpClientFactory!);
	}

	[Benchmark]
	public Task<List<ProductAlsoPurchased>> WsText()
	{
		return Run(_surrealdbWsTextClient!);
	}

	// 💡 Currently ignored benchmark: GitHub workflow need values to store benchmark results
	public Task<List<ProductAlsoPurchased>> WsBinary()
	{
		throw new NotImplementedException();
	}

	private static async Task<List<ProductAlsoPurchased>> Run(ISurrealDbClient surrealDbClient)
	{
		// Clean tables before starting
		await surrealDbClient.Delete("address");
		await surrealDbClient.Delete("customer");
		await surrealDbClient.Delete("product");
		await surrealDbClient.Delete("purchased");

		// Create records
		var pratimHomeId = new Thing("address", "pratim_home");
		var tobieHomeId = new Thing("address", "tobie_home");
		var alexHomeId = new Thing("address", "alex_home");

		var addresses = new List<Address>
		{
			new Address
			{
				Id = pratimHomeId,
				Number = "221B",
				Street = "Baker Street",
				City = "London",
				Country = "UK"
			},
			new Address
			{
				Id = tobieHomeId,
				Number = "221A",
				Street = "Church street",
				City = "London",
				Country = "UK"
			},
			new Address
			{
				Id = alexHomeId,
				Number = "221C",
				Street = "Pound street",
				City = "London",
				Country = "UK"
			}
		};

		{
			var tasks = addresses.Select(a => surrealDbClient.Create(a));
			await Task.WhenAll(tasks.ToArray());
		}

		var customers = new List<Customer>
		{
			new Customer
			{
				Id = new Thing("customer", "pratim"),
				Name = "pratim",
				Email = "abc@gmail.com",
				Address = pratimHomeId
			},
			new Customer
			{
				Id = new Thing("customer", "tobie"),
				Name = "Tobie",
				Email = "tobie@gmail.com",
				Address = tobieHomeId
			},
			new Customer
			{
				Id = new Thing("customer", "alex"),
				Name = "Alex",
				Email = "alex@gmail.com",
				Address = alexHomeId
			}
		};

		{
			var tasks = customers.Select(c => surrealDbClient.Create(c));
			await Task.WhenAll(tasks.ToArray());
		}

		var products = new List<Product>
		{
			new Product
			{
				Id = new Thing("product", "shirt"),
				Name = "Shirt",
				Description = "Slim fit",
				Price = 6,
				Category = "clothing",
				Images = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg" }
			},
			new Product
			{
				Id = new Thing("product", "trousers"),
				Name = "Trousers",
				Description = "Pants",
				Price = 10,
				Category = "clothing",
				Images = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg" }
			},
			new Product
			{
				Id = new Thing("product", "iphone"),
				Name = "Iphone",
				Description = "Mobile phone",
				Price = 600,
				Category = "Electronics",
				Images = new List<string> { "image.jpg", "image1.jpg", "image4.jpg" }
			}
		};

		{
			var tasks = products.Select(p => surrealDbClient.Create(p));
			await Task.WhenAll(tasks.ToArray());
		}

		await surrealDbClient.Query(@"
RELATE customer:pratim->purchased->product:iphone CONTENT {
    quantity: 1,
    total: 600,
    status: 'Pending',
};

RELATE customer:pratim->purchased->product:shirt CONTENT {
    quantity: 2,
    total: 40,
    status: 'Delivered',
};

RELATE customer:alex->purchased->product:iphone CONTENT {
    quantity: 1,
    total: 600,
    status: 'Pending',
};

RELATE customer:alex->purchased->product:shirt CONTENT {
    quantity: 2,
    total: 12,
    status: 'Delivered',
};

RELATE customer:tobie->purchased->product:iphone CONTENT {
    quantity: 1,
    total: 600,
    status: 'Pending',
};
		");

		// Custom query
		var customQueryResponse = await surrealDbClient.Query(@"
SELECT
	id,
	array::distinct(<-purchased<-customer->purchased->product.*) AS purchases
FROM product:shirt;"
		);

		return customQueryResponse.GetValue<List<ProductAlsoPurchased>>(0)!;
	}
}
