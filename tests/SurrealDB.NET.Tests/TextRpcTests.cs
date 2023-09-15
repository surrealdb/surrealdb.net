using System.Reflection;
using System.Text.Json.Serialization;

using SurrealDB.NET.Json;
using SurrealDB.NET.Tests.Fixtures;

using Xunit.Abstractions;

namespace SurrealDB.NET.Tests;

[Trait("Category", "Text RPC Methods")]
public sealed class TextRpcTests : IClassFixture<SurrealDbFixture>, IDisposable
{
	private readonly SurrealDbFixture _fixture;
	private readonly ServiceFixture _di;

	public TextRpcTests(SurrealDbFixture fixture, ITestOutputHelper xunit)
	{
		_fixture = fixture;
		_di = new ServiceFixture(xunit, _fixture.Port);
	}

	public void Dispose()
	{
		_di.Dispose();
	}

	[Fact(DisplayName = "UseAsync", Timeout = 5000)]
	public async Task UseAsync()
	{
		await _di.Client.UseAsync("test", "test").ConfigureAwait(false);
	}

	[Fact(DisplayName = "InfoAsyncWithoutScope", Timeout = 5000)]
	public async Task InfoAsyncWithoutScope()
	{
		_ = await _di.Client.SigninRootAsync("root", "root").ConfigureAwait(false);
		_ = await _di.Client.InfoAsync<User>().ConfigureAwait(false);
	}

	[Fact(DisplayName = "SigninRootAsync", Timeout = 5000)]
	public async Task SigninRootAsync()
	{
		await _di.Client.SigninRootAsync("root", "root").ConfigureAwait(false);
	}

	[Fact(DisplayName = "SignupAsync", Timeout = 5000)]
	public async Task SignupAsync()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		await _di.Client.SigninRootAsync("root", "root").ConfigureAwait(false);
		var token = await _di.Client.SignupAsync("test", "test", "account", user).ConfigureAwait(false);
		Assert.NotNull(token);
		Assert.NotEmpty(token);
	}

	[Fact(DisplayName = "SigninScopeAsync", Timeout = 5000)]
	public async Task SigninScopeAsync()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		var signupToken = await _di.Client.SignupAsync("test", "test", "account", user).ConfigureAwait(false);
		var signinToken = await _di.Client.SigninScopeAsync("test", "test", "account", user).ConfigureAwait(false);

		Assert.NotNull(signupToken);
		Assert.NotNull(signinToken);
		Assert.NotEmpty(signupToken);
		Assert.NotEmpty(signinToken);
	}

	[Fact(DisplayName = "AuthenticateAsync", Timeout = 5000)]
	public async Task AuthenticateAsync()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		var signupToken = await _di.Client.SignupAsync("test", "test", "account", user).ConfigureAwait(false);
		Assert.NotNull(signupToken);
		await _di.Client.AuthenticateAsync(signupToken).ConfigureAwait(false);
	}

	[Fact(DisplayName = "InvalidateAsync", Timeout = 5000)]
	public async Task InvalidateAsync()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		var signupToken = await _di.Client.SignupAsync("test", "test", "account", user).ConfigureAwait(false);
		Assert.NotNull(signupToken);
		await _di.Client.AuthenticateAsync(signupToken).ConfigureAwait(false);
		await _di.Client.InvalidateAsync().ConfigureAwait(false);
		user = await _di.Client.InfoAsync<User>().ConfigureAwait(false);
		Assert.Null(user);
	}

	[Fact(DisplayName = "LetAsync", Timeout = 5000)]
	public async Task LetAsync()
	{
		var value = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
		await _di.Client.LetAsync("xunit", value).ConfigureAwait(false);
		var result = await _di.Client.QueryAsync("RETURN $xunit").ConfigureAwait(false);
		var xunit = result.Get<string>(0);
		Assert.Equal(value, xunit);
	}

	[Fact(DisplayName = "UnsetAsync", Timeout = 5000)]
	public async Task UnsetAsync()
	{
		var value = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
		await _di.Client.LetAsync("xunit", value).ConfigureAwait(false);
		var result1 = await _di.Client.QueryAsync("RETURN $xunit").ConfigureAwait(false);
		Assert.Equal(value, result1.Get<string>(0));
		await _di.Client.UnsetAsync("xunit").ConfigureAwait(false);
		var result2 = await _di.Client.QueryAsync("RETURN $xunit").ConfigureAwait(false);
		Assert.Null(result2.Get<string>(0));
	}

	[Fact(DisplayName = "LiveAsync", Timeout = 5000)]
	public async Task LiveAsync()
	{
		var notification = new TaskCompletionSource<(Post, SurrealEventType)>();
		var liveId = await _di.Client.LiveAsync<Post>("post", (post, type) =>
		{
			notification.SetResult((post, type));

		}).ConfigureAwait(false);
		Assert.NotEqual(new SurrealLiveQueryId(), liveId);
		var inserted = await _di.Client.InsertAsync("post", new Post("SurrealDB is pretty cool")).ConfigureAwait(false);
		Assert.NotNull(inserted);
		var (notifiedPost, notifiedType) = await notification.Task.ConfigureAwait(false);
		Assert.NotNull(notifiedPost);
		Assert.Equal(inserted.Content, notifiedPost.Content);
		Assert.Equal(SurrealEventType.Create, notifiedType);
	}

	[Fact(DisplayName = "KillAsync", Timeout = 5000)]
	public async Task KillAsync()
	{
		var tcs = new TaskCompletionSource<(Post, SurrealEventType)>();
		var live = await _di.Client.LiveAsync<Post>("post", (post, type) =>
		{
			tcs.SetResult((post, type));

		}).ConfigureAwait(false);
		await _di.Client.KillAsync(live).ConfigureAwait(false);
		var timeout = Task.Delay(2500);
		var t = await Task.WhenAny(tcs.Task, timeout).ConfigureAwait(false);
		Assert.False(t == tcs.Task);
		Assert.True(t == timeout);
		Assert.Null(t.Exception);
		Assert.Null(timeout.Exception);
	}

	[Fact(DisplayName = "QueryAsyncSingleOnlyFound", Timeout = 5000)]
	public async Task QueryAsyncSingleOnlyFound()
	{
		var content = "Querying a single record is simple!";
		var id = new Thing
		{
			Table = "post",
			Id = nameof(QueryAsyncSingleOnlyFound),
		};
		var created = await _di.Client.CreateAsync(id, new Post(content)).ConfigureAwait(false);
		Assert.NotNull(created);
		Assert.Equal(id, created.Id);
		var result = await _di.Client.QueryAsync("SELECT * FROM ONLY type::thing($id)", new { id = created.Id }).ConfigureAwait(false);
		var post = result.Get<Post>();
		Assert.NotNull(post);
		Assert.Equal(created.Id, post.Id);
		Assert.Equal(created.Content, post.Content);
	}

	[Fact(DisplayName = "query on no results with the ONLY keyword should throw an exception", Timeout = 5000)]
	public async Task Query_SingleOnNone()
	{
		var result = await _di.Client.QueryAsync("SELECT * FROM ONLY type::thing($id)", new
		{
			id = new Thing
			{
				Table = "post",
				Id = "NotFound",
			}

		}).ConfigureAwait(false);
		Assert.Throws<InvalidOperationException>(() => result.Get<Post>());
	}

	[Fact(DisplayName = "query on no results should yield an empty collection", Timeout = 5000)]
	public async Task Query_ManyOnNone()
	{
		var result = await _di.Client.QueryAsync("SELECT * FROM type::thing($id)", new
		{
			id = new Thing
			{
				Table = "post",
				Id = nameof(Query_ManyOnNone),
			}
		}).ConfigureAwait(false);

		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.Empty(posts);
	}

	[Fact(DisplayName = "QueryAsyncMultipleFound", Timeout = 5000)]
	public async Task QueryAsyncMultipleFound()
	{
		var inserted = await _di.Client.InsertAsync("post", new Post[]
		{
			new(nameof(QueryAsyncMultipleFound) + "1"),
			new(nameof(QueryAsyncMultipleFound) + "2"),

		}).ConfigureAwait(false);
		var result = await _di.Client.QueryAsync("SELECT * FROM post").ConfigureAwait(false);
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.True(posts.Count() > 1);
	}

	[Fact(DisplayName = "QueryAsyncMultipleNone", Timeout = 5000)]
	public async Task QueryAsyncMultipleNone()
	{
		var result = await _di.Client.QueryAsync("SELECT * FROM post WHERE content == $p", new
		{
			p = "NotFound"

		}).ConfigureAwait(false);
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.True(posts.Count() is 0);
	}

	[Fact(DisplayName = "select thing that exists", Timeout = 5000)]
	public async Task SelectOneFound()
	{
		var content = "SurrealDB is pretty cool";
		var id = new Thing
		{
			Table = "post",
			Id = nameof(SelectOneFound)
		};
		var created = await _di.Client.CreateAsync(id, new Post(content)).ConfigureAwait(false);
		Assert.NotNull(created);
		var result = await _di.Client.SelectAsync<Post>(id).ConfigureAwait(false);
		Assert.NotNull(result);
		Assert.Equal(content, result.Content);
	}

	[Fact(DisplayName = "select thing that does not exist", Timeout = 5000)]
	public async Task SelectOneNotFound()
	{
		var id = new Thing
		{
			Table = "post",
			Id = "NotFound"
		};
		var result = await _di.Client.SelectAsync<Post>(id).ConfigureAwait(false);
		Assert.Null(result);
	}

	[Fact(DisplayName = "create with manual id", Timeout = 5000)]
	public async Task CreateWithManualId()
	{
		var manual = new Thing
		{
			Table = "post",
			Id = "ManualId",
		};
		var created = await _di.Client.CreateAsync(manual, new Post("Creating with manual id")).ConfigureAwait(false);
		Assert.NotNull(created);
	}

	[Fact(DisplayName = "InsertOneAsync", Timeout = 5000)]
	public async Task InsertOneAsync()
	{
		var content = "SurrealDB is pretty cool";
		var inserted = await _di.Client.InsertAsync("post", new Post(content)).ConfigureAwait(false);
		Assert.NotNull(inserted);
		Assert.Equal(content, inserted.Content);
	}

	[Fact(DisplayName = "InsertManyAsync", Timeout = 5000)]
	public async Task InsertManyAsync()
	{
		var content = "SurrealDB is pretty cool";
		var inserted = await _di.Client.InsertAsync("post", new Post[]
		{
			new(content),
			new(content),

		}).ConfigureAwait(false);

		Assert.NotNull(inserted);
		Assert.True(inserted.Length is 2);
	}

	[Fact(DisplayName = "update on existent record fully overwrites the record", Timeout = 5000)]
	public async Task UpdateExistingRecord()
	{
		var id = new Thing
		{
			Table = "post",
			Id = "update",
		};
		var post = new Post("Update this later!");

		var created = await _di.Client.CreateAsync(id, post).ConfigureAwait(false);
		var updated = await _di.Client.UpdateAsync(id, created with
		{
			Content = "New content!"

		}).ConfigureAwait(false);

		Assert.Equal("New content!", updated.Content);
	}

	[Fact(DisplayName = "update on non-existent record upserts it", Timeout = 5000)]
	public async Task UpdateNonExistentRecordFunctionsAsUpsert()
	{
		var id = new Thing
		{
			Table = "post",
			Id = "upsert",
		};
		var post = new Post("Upsert this!");
		var upserted = await _di.Client.UpdateAsync(id, post).ConfigureAwait(false);

		Assert.Equal("Upsert this!", upserted.Content);
	}

	[Fact(DisplayName = "merge record that exists should update the record", Timeout = 5000)]
	public async Task MergeAsync()
	{
		var id = new Thing
		{
			Table = "post",
			Id = nameof(MergeAsync),
		};

		var created = await _di.Client.CreateAsync(id, new Post("This post will be merged later")).ConfigureAwait(false);
		Assert.NotNull(created);
		const string tags = "surrealdb,merge,tests";
		var merged = await _di.Client.MergeAsync<Post>(id, new
		{
			tags

		}).ConfigureAwait(false);
		Assert.NotNull(merged);
		Assert.Equal(tags, merged.Tags);
	}

	[Fact(DisplayName = "merge record that does not exist should upsert the record", Timeout = 5000)]
	public async Task MergeUpsertAsync()
	{
		var id = new Thing
		{
			Table = "post",
			Id = nameof(MergeUpsertAsync),
		};

		const string tags = "surrealdb,merge,tests";
		var merged = await _di.Client.MergeAsync<Post>(id, new
		{
			content = "redacted",
			tags

		}).ConfigureAwait(false);
		Assert.NotNull(merged);
		Assert.Equal(tags, merged.Tags);
	}

	[Fact(DisplayName = "merge table should update all records", Timeout = 5000)]
	public async Task MergeAllAsync()
	{
		var ids = Enumerable.Range(1, 3).Select(i => new Thing
		{
			Table = "post",
			Id = $"{nameof(MergeAllAsync)}{i}",
		});

		foreach (var id in ids)
		{
			var created = await _di.Client.CreateAsync(id, new Post($"Post #{id.Id}")).ConfigureAwait(false);
			Assert.NotNull(created);
		}

		const string tags = "surrealdb,merge,tests";
		var merged = await _di.Client.MergeAsync<Post>("post", new
		{
			tags

		}).ConfigureAwait(false);

		Assert.NotEmpty(merged);

		foreach (var m in merged)
			Assert.Equal(tags, m.Tags);
	}

	// TODO: patch
	// TODO: delete
}

file sealed record User(
	[property: JsonPropertyName("email")]
	string Email,
	[property: JsonPropertyName("password")]
	string Password)
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonConverter(typeof(SurrealRecordIdJsonConverter))]
	public Thing Id { get; init; }
};

file sealed record Post(
	[property: JsonPropertyName("content")]
	string Content)
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonConverter(typeof(SurrealRecordIdJsonConverter))]
	public Thing Id { get; init; }

	[JsonPropertyName("tags")]
	public string? Tags { get; init; }
};
