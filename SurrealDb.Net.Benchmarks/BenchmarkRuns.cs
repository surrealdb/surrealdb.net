using System.Runtime.CompilerServices;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks;

public static class BenchmarkRuns
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<Post> Create(ISurrealDbClient surrealDbClient, PostFaker postFaker)
    {
        var generatedPost = postFaker!.Generate();
        var post = new Post { Title = generatedPost.Title, Content = generatedPost.Content };

        return surrealDbClient.Create("post", post);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task Delete(ISurrealDbClient surrealDbClient)
    {
        return surrealDbClient.Delete("post");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<List<Post>> Query(ISurrealDbClient surrealDbClient)
    {
        var response = await surrealDbClient.Query(
            @$"
    SELECT * FROM post;
    SELECT * FROM $auth;

    BEGIN TRANSACTION;

    CREATE post;

    CANCEL TRANSACTION;"
        );

        var posts = response.GetValues<Post>(0)!;
        return posts.ToList();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<List<ProductAlsoPurchased>> Scenario(ISurrealDbClient surrealDbClient)
    {
        // Clean tables before starting
        await surrealDbClient.Delete("address");
        await surrealDbClient.Delete("customer");
        await surrealDbClient.Delete("product");
        await surrealDbClient.Delete("purchased");

        // Create records
        var pratimHomeId = new RecordIdOfString("address", "pratim_home");
        var tobieHomeId = new RecordIdOfString("address", "tobie_home");
        var alexHomeId = new RecordIdOfString("address", "alex_home");

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
                Id = ("customer", "pratim"),
                Name = "pratim",
                Email = "abc@gmail.com",
                Address = pratimHomeId
            },
            new Customer
            {
                Id = ("customer", "tobie"),
                Name = "Tobie",
                Email = "tobie@gmail.com",
                Address = tobieHomeId
            },
            new Customer
            {
                Id = ("customer", "alex"),
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
                Id = ("product", "shirt"),
                Name = "Shirt",
                Description = "Slim fit",
                Price = 6,
                Category = "clothing",
                Images = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg" }
            },
            new Product
            {
                Id = ("product", "trousers"),
                Name = "Trousers",
                Description = "Pants",
                Price = 10,
                Category = "clothing",
                Images = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg" }
            },
            new Product
            {
                Id = ("product", "iphone"),
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<List<Post>> Select(ISurrealDbClient surrealDbClient)
    {
        var result = await surrealDbClient.Select<Post>("post");
        return result.ToList();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<Post> Upsert(
        ISurrealDbClient surrealDbClient,
        PostFaker postFaker,
        Post post
    )
    {
        var generatedPost = postFaker.Generate();

        post.Title = generatedPost.Title;
        post.Content = generatedPost.Content;

        return surrealDbClient.Upsert(post);
    }
}
