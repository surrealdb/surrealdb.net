using SurrealDB.NET.Errors;
using SurrealDB.NET.Tests.Fixtures;
using SurrealDB.NET.Tests.Schema;
using Xunit.Abstractions;

namespace SurrealDB.NET.Tests;

[Trait("Category", "HTTP")]
[Collection(SurrealDbCollectionFixture.Name)]
public sealed class SurrealHttpTests : IDisposable
{
	private readonly SurrealDbFixture _fixture;
	private readonly ServiceFixture _di;

	public SurrealHttpTests(SurrealDbFixture fixture, ITestOutputHelper xunit)
	{
		_fixture = fixture;
		_di = new ServiceFixture( xunit, _fixture.Port);
	}

	public void Dispose()
	{
		_di.Dispose();
	}

	[Fact(DisplayName = "GET /export", Timeout = 15000)]
	public async Task Export()
	{
		var id = Guid.NewGuid();
		var filename = $"export-test-{id}.surql";
		if (File.Exists(filename))
			File.Delete(filename);

		{
			var root = _di.Http;
			root.AttachToken(await root.SigninRootAsync("root", "root"));
			using var fileStream = File.OpenWrite(filename);
			await root.ExportAsync(fileStream);
		}
		
		using var assertStream = File.OpenRead(filename);
		Assert.True(assertStream.Length > 0);
	}

	[Fact(DisplayName = "GET /health on healthy service", Timeout = 5000)]
	public async Task Health()
	{
		var health = await _di.Http.HealthAsync();

		Assert.True(health);
	}

	[Fact(DisplayName = "POST /import", Timeout = 15000)]
	public async Task Import()
	{
		var filename = @"C:\dev\s-e-f\surrealdb.net\tests\SurrealDB.NET.Tests\Data\surreal_deal_v1.surql";
		using var s = File.OpenRead(filename);
		var root = _di.Http;
		var token = await root.SigninRootAsync("root", "root");
		root.AttachToken(token);
		await root.ImportAsync(s);
	}

	[Fact(DisplayName = "GET /key/:table on multiple records", Timeout = 5000)]
	public async Task Get()
	{
		var create = await _di.TextRpc.InsertAsync("post", new Post[]
		{
			new(nameof(Get) + "1"),
			new(nameof(Get) + "2"),
			new(nameof(Get) + "3")
		});

		var records = await _di.Http.GetAllAsync<Post>("post");
		Assert.True(records.Count() >= 3);
	}

	[Fact(DisplayName = "POST /key/:table", Timeout = 5000)]
	public async Task Post()
	{
		var created = await _di.Http.InsertAsync("post", new Post(nameof(Post)));
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
		var created = await _di.Http.InsertAsync("post", new Post(nameof(GetById)));
		Assert.NotNull(created);

		var get = await _di.Http.GetAsync<Post>(created.Id);
		Assert.Equal(created.Content, get?.Content);
	}

	[Fact(DisplayName = "POST /key/:table/:id", Timeout = 5000)]
	public async Task PostById()
	{
		var created = await _di.Http.CreateAsync($"post:{nameof(PostById)}", new Post(nameof(PostById)));
		Assert.NotNull(created);
	}

	[Fact(DisplayName = "PUT /key/:table/:id", Timeout = 5000)]
	public async Task PutById()
	{
		var created = await _di.Http.InsertAsync("post", new Post(nameof(PutById)));
		Assert.NotNull(created);

		const string updatedContent = "Updated content";
		var updated = await _di.Http.UpdateAsync(created.Id, new Post(updatedContent));
		Assert.NotNull(updated);
		Assert.Equal(updatedContent, updated.Content);
	}

	[Fact(DisplayName = "PATCH /key/:table/:id", Timeout = 5000)]
	public async Task PatchById()
	{
		var created = await _di.Http.InsertAsync("post", new Post(nameof(PatchById)));
		Assert.NotNull(created);

		const string tags = "tags here";
		var updated = await _di.Http.MergeAsync<Post>(created.Id, new
		{
			tags
		});
		Assert.NotNull(updated);
		Assert.Equal(tags, updated.Tags);
	}

	[Fact(DisplayName = "DELETE /key/:table/:id", Timeout = 5000)]
	public async Task DeleteById()
	{
		await _di.Http.SigninRootAsync("root", "root");

		var created = await _di.Http.InsertAsync("post", new Post(nameof(DeleteById)));
		Assert.NotNull(created);

		var deleted = await _di.Http.DeleteAsync<Post>(created.Id);
		Assert.Equal(created.Content, deleted?.Content);
	}

	[Fact(DisplayName = "POST /signup", Timeout = 5000)]
	public async Task Signup()
	{
		var token = await _di.Http.SignupAsync("test", "test", "account", new User("test@test.com", "httppassword"));
		Assert.False(string.IsNullOrWhiteSpace(token));
	}

	[Fact(DisplayName = "POST /signin as scope", Timeout = 5000)]
	public async Task SigninScope()
	{
		var user = new User($"{nameof(SigninScope)}@test.com", "httppassword");
		var signupToken = await _di.Http.SignupAsync("test", "test", "account", user);
		var signinToken = await _di.Http.SigninScopeAsync("test", "test", "account", user);
		Assert.False(string.IsNullOrWhiteSpace(signinToken));
	}

	[Fact(DisplayName = "POST /signin as namespace", Timeout = 5000)]
	public async Task SigninNamespace()
	{
		var token = await _di.Http.SigninNamespaceAsync("test", "test_root", "test_root");
		Assert.False(string.IsNullOrWhiteSpace(token));
	}

	[Fact(DisplayName = "POST /signin as root", Timeout = 5000)]
	public async Task SigninRoot()
	{
		var token = await _di.Http.SigninRootAsync("root", "root");
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
		var created = await _di.Http.CreateAsync(id, new Post(content));
		Assert.NotNull(created);
		Assert.Equal(id, created.Id);
		var result = await _di.Http.QueryAsync("SELECT * FROM ONLY type::thing($id)", new { id = created.Id });
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
			_ = await _di.Http.QueryAsync("SELECT * FROM ONLY type::thing($id)", new
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
		var result = await _di.Http.QueryAsync("SELECT * FROM type::thing($id)", new
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
		var inserted = await _di.TextRpc.InsertAsync("post", new Post[]
		{
			new(nameof(SqlMultipleWithManyResults) + "1"),
			new(nameof(SqlMultipleWithManyResults) + "2"),

		});
		var result = await _di.Http.QueryAsync("SELECT * FROM post");
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.True(posts.Count() > 1);
	}

	[Fact(DisplayName = "POST /sql multiple on empty should return empty collection", Timeout = 5000)]
	public async Task SqlMultipleOnEmpty()
	{
		var result = await _di.Http.QueryAsync("SELECT * FROM post WHERE content == $p", new
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
		var status = await _di.Http.StatusAsync();
		Assert.True(status);
	}

	[Fact(DisplayName = "GET /version", Timeout = 5000)]
	public async Task Version()
	{
		var version = await _di.Http.VersionAsync();
		Assert.False(string.IsNullOrWhiteSpace(version));
	}
}
