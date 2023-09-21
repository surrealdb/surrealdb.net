using SurrealDB.NET.Errors;
using SurrealDB.NET.Http;
using SurrealDB.NET.Tests.Fixtures;
using SurrealDB.NET.Tests.Schema;

namespace SurrealDB.NET.Tests;

[Trait("Category", "HTTP")]
[Collection(SurrealDbCollectionFixture.Name)]
public sealed class SurrealHttpTests
{
	private readonly SurrealDbCliFixture _fixture;
	private ISurrealHttpClient _client => _fixture.Http;

	public SurrealHttpTests(SurrealDbCliFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact(DisplayName = "GET /export", Timeout = 15000)]
	public async Task Export()
	{
		var id = Guid.NewGuid();
		var filename = $"export-test-{id}.surql";
		if (File.Exists(filename))
			File.Delete(filename);

		{
			_client.AttachToken(await _client.SigninRootAsync("root", "root"));
			using var fileStream = File.OpenWrite(filename);
			await _client.ExportAsync(fileStream);
		}
		
		using var assertStream = File.OpenRead(filename);
		Assert.True(assertStream.Length > 0);
	}

	[Fact(DisplayName = "GET /health on healthy service", Timeout = 5000)]
	public async Task Health()
	{
		var health = await _client.HealthAsync();

		Assert.True(health);
	}

	[Fact(DisplayName = "POST /import", Timeout = 15000)]
	public async Task Import()
	{
		var filename = @"C:\dev\s-e-f\surrealdb.net\tests\SurrealDB.NET.Tests\Data\surreal_deal_v1.surql";
		using var s = File.OpenRead(filename);
		var root = _client;
		var token = await root.SigninRootAsync("root", "root");
		root.AttachToken(token);
		await root.ImportAsync(s);
	}

	[Fact(DisplayName = "GET /key/:table on multiple records", Timeout = 5000)]
	public async Task Get()
	{
		await _client.CreateAsync("post", new Post(nameof(Get) + "1"));
		await _client.CreateAsync("post", new Post(nameof(Get) + "2"));
		await _client.CreateAsync("post", new Post(nameof(Get) + "3"));

		var records = await _client.GetAllAsync<Post>("post");
		Assert.True(records.Count() >= 3);
	}

	[Fact(DisplayName = "POST /key/:table", Timeout = 5000)]
	public async Task Post()
	{
		var created = await _client.InsertAsync("post", new Post(nameof(Post)));
		Assert.NotNull(created);
	}

	[Fact(DisplayName = "DELETE /key/:table", Timeout = 5000)]
	public Task Delete()
	{
		// TODO: Separate table for dropping that does not conflict with other tests
		return Task.CompletedTask;
	}

	[Fact(DisplayName = "GET /key/:table/:id", Timeout = 5000)]
	public async Task GetById()
	{
		var created = await _client.InsertAsync("post", new Post(nameof(GetById)));
		Assert.NotNull(created);

		var get = await _client.GetAsync<Post>(created.Id);
		Assert.Equal(created.Content, get?.Content);
	}

	[Fact(DisplayName = "POST /key/:table/:id", Timeout = 5000)]
	public async Task PostById()
	{
		var created = await _client.CreateAsync($"post:{nameof(PostById)}", new Post(nameof(PostById)));
		Assert.NotNull(created);
	}

	[Fact(DisplayName = "PUT /key/:table/:id", Timeout = 5000)]
	public async Task PutById()
	{
		var created = await _client.InsertAsync("post", new Post(nameof(PutById)));
		Assert.NotNull(created);

		const string updatedContent = "Updated content";
		var updated = await _client.UpdateAsync(created.Id, new Post(updatedContent));
		Assert.NotNull(updated);
		Assert.Equal(updatedContent, updated.Content);
	}

	[Fact(DisplayName = "PATCH /key/:table/:id", Timeout = 5000)]
	public async Task PatchById()
	{
		var created = await _client.InsertAsync("post", new Post(nameof(PatchById)));
		Assert.NotNull(created);

		const string tags = "tags here";
		var updated = await _client.MergeAsync<Post>(created.Id, new
		{
			tags
		});
		Assert.NotNull(updated);
		Assert.Equal(tags, updated.Tags);
	}

	[Fact(DisplayName = "DELETE /key/:table/:id", Timeout = 5000)]
	public async Task DeleteById()
	{
		await _client.SigninRootAsync("root", "root");

		var created = await _client.InsertAsync("post", new Post(nameof(DeleteById)));
		Assert.NotNull(created);

		var deleted = await _client.DeleteAsync<Post>(created.Id);
		Assert.Equal(created.Content, deleted?.Content);
	}

	[Fact(DisplayName = "POST /signup", Timeout = 5000)]
	public async Task Signup()
	{
		var token = await _client.SignupAsync("test", "test", "account", new User("test@test.com", "httppassword"));
		Assert.False(string.IsNullOrWhiteSpace(token));
	}

	[Fact(DisplayName = "POST /signin as scope", Timeout = 5000)]
	public async Task SigninScope()
	{
		var user = new User($"{nameof(SigninScope)}@test.com", "httppassword");
		var signupToken = await _client.SignupAsync("test", "test", "account", user);
		var signinToken = await _client.SigninScopeAsync("test", "test", "account", user);
		Assert.False(string.IsNullOrWhiteSpace(signinToken));
	}

	[Fact(DisplayName = "POST /signin as namespace", Timeout = 5000)]
	public async Task SigninNamespace()
	{
		var token = await _client.SigninNamespaceAsync("test", "test_root", "test_root");
		Assert.False(string.IsNullOrWhiteSpace(token));
	}

	[Fact(DisplayName = "POST /signin as root", Timeout = 5000)]
	public async Task SigninRoot()
	{
		var token = await _client.SigninRootAsync("root", "root");
		Assert.False(string.IsNullOrWhiteSpace(token));
	}


	[Fact(DisplayName = "POST /sql with the ONLY clause on record that exists", Timeout = 5000)]
	public async Task SqlSingleOnlyExists()
	{
		var content = "Querying a single record is simple!";
		var id = new Thing
		{
			Table = "post",
			Id = nameof(SqlSingleOnlyExists),
		};
		var created = await _client.CreateAsync(id, new Post(content));
		Assert.NotNull(created);
		Assert.Equal(id, created.Id);
		var result = await _client.QueryAsync("SELECT * FROM ONLY type::thing($id)", new { id = created.Id });
		var post = result.Get<Post>();
		Assert.NotNull(post);
		Assert.Equal(created.Id, post.Id);
		Assert.Equal(created.Content, post.Content);
	}

	[Fact(DisplayName = "POST /sql with the ONLY clause on non-existent record should throw an exception", Timeout = 5000)]
	public async Task SqlSingleOnlyNotFound()
	{
		var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
		{
			_ = await _client.QueryAsync("SELECT * FROM ONLY type::thing($id)", new
			{
				id = new Thing
				{
					Table = "post",
					Id = "NotFound",
				}

			});
		});

		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "POST /sql on no results should yield an empty collection", Timeout = 5000)]
	public async Task SqlOnNone()
	{
		var result = await _client.QueryAsync("SELECT * FROM type::thing($id)", new
		{
			id = new Thing
			{
				Table = "post",
				Id = nameof(SqlOnNone),
			}
		});

		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.Empty(posts);
	}

	[Fact(DisplayName = "POST /sql on many records should return them", Timeout = 5000)]
	public async Task SqlMultipleWithManyResults()
	{
		await _client.InsertAsync("post", new Post(nameof(SqlMultipleWithManyResults) + "1"));
		await _client.InsertAsync("post", new Post(nameof(SqlMultipleWithManyResults) + "2"));
		var result = await _client.QueryAsync("SELECT * FROM post");
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.True(posts.Count() > 1);
	}

	[Fact(DisplayName = "POST /sql multiple on empty should return empty collection", Timeout = 5000)]
	public async Task SqlMultipleOnEmpty()
	{
		var result = await _client.QueryAsync("SELECT * FROM post WHERE content == $p", new
		{
			p = "NotFound"

		});
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.Empty(posts);
	}

	[Fact(DisplayName = "GET /status", Timeout = 5000)]
	public async Task Status()
	{
		var status = await _client.StatusAsync();
		Assert.True(status);
	}

	[Fact(DisplayName = "GET /version", Timeout = 5000)]
	public async Task Version()
	{
		var version = await _client.VersionAsync();
		Assert.False(string.IsNullOrWhiteSpace(version));
	}
}
