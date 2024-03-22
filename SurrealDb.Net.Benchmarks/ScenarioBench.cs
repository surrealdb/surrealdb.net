using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks;

public class ScenarioBench : BaseBenchmark
{
    private readonly SurrealDbClientGenerator[] _surrealDbClientGenerators =
        new SurrealDbClientGenerator[4];

    private ISurrealDbClient? _surrealdbHttpClient;
    private ISurrealDbClient? _surrealdbHttpClientWithHttpClientFactory;
    private ISurrealDbClient? _surrealdbWsTextClient;
    private ISurrealDbClient? _surrealdbWsBinaryClient;

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
                    _surrealdbHttpClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(HttpUrl)
                            .WithNamingPolicy(NamingPolicy)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClient, dbInfo);
                    await _surrealdbHttpClient.Connect();
                    break;
                case 1:
                    _surrealdbHttpClientWithHttpClientFactory = clientGenerator.Create(
                        $"Endpoint={HttpUrl}",
                        funcJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbHttpClientWithHttpClientFactory, dbInfo);
                    await _surrealdbHttpClientWithHttpClientFactory.Connect();
                    break;
                case 2:
                    _surrealdbWsTextClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(WsUrl)
                            .WithNamingPolicy(NamingPolicy)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbWsTextClient, dbInfo);
                    await _surrealdbWsTextClient.Connect();
                    break;
                case 3:
                    _surrealdbWsBinaryClient = new SurrealDbClient(
                        SurrealDbOptions
                            .Create()
                            .WithEndpoint(WsUrl)
                            .WithNamingPolicy(NamingPolicy)
                            .WithSerialization(SerializationConstants.CBOR)
                            .Build(),
                        appendJsonSerializerContexts: GetFuncJsonSerializerContexts()
                    );
                    InitializeSurrealDbClient(_surrealdbWsBinaryClient, dbInfo);
                    await _surrealdbWsBinaryClient.Connect();
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
        _surrealdbWsBinaryClient?.Dispose();
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

    [Benchmark]
    public Task<List<ProductAlsoPurchased>> WsBinary()
    {
        return Run(_surrealdbWsBinaryClient!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
            new()
            {
                Id = pratimHomeId,
                Number = "221B",
                Street = "Baker Street",
                City = "London",
                Country = "UK"
            },
            new()
            {
                Id = tobieHomeId,
                Number = "221A",
                Street = "Church street",
                City = "London",
                Country = "UK"
            },
            new()
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

        await surrealDbClient.RawQuery(
            @"
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
		"
        );

        // Custom query
        var customQueryResponse = await surrealDbClient.RawQuery(
            @"
SELECT
	id,
	array::distinct(<-purchased<-customer->purchased->product.*) AS purchases
FROM product:shirt;"
        );
        var customQueryResult = customQueryResponse.FirstOk;

        return customQueryResult!.GetValues<ProductAlsoPurchased>().ToList();
    }
}
