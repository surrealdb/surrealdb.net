using System.Reflection;
using SurrealDB.NET.Errors;
using SurrealDB.NET.Rpc;
using SurrealDB.NET.Tests.Fixtures;
using SurrealDB.NET.Tests.Schema;
using Xunit.Abstractions;

namespace SurrealDB.NET.Tests;

[Trait("Category", "Text RPC")]
[Collection(SurrealDbCollectionFixture.Name)]
public sealed class SurrealTextRpcTests : IDisposable
{
	private readonly SurrealDbFixture _fixture;
	private readonly ServiceFixture _di;

	public SurrealTextRpcTests(SurrealDbFixture fixture, ITestOutputHelper xunit)
	{
		_fixture = fixture;
		_di = new ServiceFixture(xunit, _fixture.Port);
	}

	public void Dispose()
	{
		_di.Dispose();
	}

	[Fact(DisplayName = "use", Timeout = 5000)]
	public async Task Use()
	{
		await _di.TextRpc.UseAsync("test", "test");
	}

	[Fact(DisplayName = "info without scope", Timeout = 5000)]
	public async Task InfoWithoutScope()
	{
		_ = await _di.TextRpc.SigninRootAsync("root", "root");
		_ = await _di.TextRpc.InfoAsync<User>();
	}

	[Fact(DisplayName = "signin as root", Timeout = 5000)]
	public async Task SigninRoot()
	{
		await _di.TextRpc.SigninRootAsync("root", "root");
	}

	[Fact(DisplayName = "signup", Timeout = 5000)]
	public async Task Signup()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		await _di.TextRpc.SigninRootAsync("root", "root");
		var token = await _di.TextRpc.SignupAsync("test", "test", "account", user);
		Assert.NotNull(token);
		Assert.NotEmpty(token);
	}

	[Fact(DisplayName = "signin as scope", Timeout = 5000)]
	public async Task SigninScope()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		var signupToken = await _di.TextRpc.SignupAsync("test", "test", "account", user);
		var signinToken = await _di.TextRpc.SigninAsync("test", "test", "account", user);

		Assert.NotNull(signupToken);
		Assert.NotNull(signinToken);
		Assert.NotEmpty(signupToken);
		Assert.NotEmpty(signinToken);
	}

	[Fact(DisplayName = "authenticate", Timeout = 5000)]
	public async Task Authenticate()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		var signupToken = await _di.TextRpc.SignupAsync("test", "test", "account", user);
		Assert.NotNull(signupToken);
		await _di.TextRpc.AuthenticateAsync(signupToken);
	}

	[Fact(DisplayName = "invalidate", Timeout = 5000)]
	public async Task Invalidate()
	{
		var user = new User($"{Guid.NewGuid()}@testing.com", "testing");
		var signupToken = await _di.TextRpc.SignupAsync("test", "test", "account", user);
		Assert.NotNull(signupToken);
		await _di.TextRpc.AuthenticateAsync(signupToken);
		await _di.TextRpc.InvalidateAsync();
		user = await _di.TextRpc.InfoAsync<User>();
		Assert.Null(user);
	}

	[Fact(DisplayName = "let", Timeout = 5000)]
	public async Task Let()
	{
		var value = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
		await _di.TextRpc.LetAsync("xunit", value);
		var result = await _di.TextRpc.QueryAsync("RETURN $xunit");
		var xunit = result.Get<string>(0);
		Assert.Equal(value, xunit);
	}

	[Fact(DisplayName = "unset", Timeout = 5000)]
	public async Task Unset()
	{
		var value = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
		await _di.TextRpc.LetAsync("xunit", value);
		var result1 = await _di.TextRpc.QueryAsync("RETURN $xunit");
		Assert.Equal(value, result1.Get<string>(0));
		await _di.TextRpc.UnsetAsync("xunit");
		var result2 = await _di.TextRpc.QueryAsync("RETURN $xunit");
		Assert.Null(result2.Get<string>(0));
	}

	[Fact(DisplayName = "live", Timeout = 5000)]
	public async Task Live()
	{
		var notification = new TaskCompletionSource<(Post, SurrealEventType)>();
		var liveId = await _di.TextRpc.LiveAsync<Post>("post", (post, type) =>
		{
			notification.SetResult((post, type));

		});
		Assert.NotEqual(new SurrealLiveQueryId(), liveId);
		var inserted = await _di.TextRpc.InsertAsync("post", new Post("SurrealDB is pretty cool"));
		Assert.NotNull(inserted);
		var (notifiedPost, notifiedType) = await notification.Task;
		Assert.NotNull(notifiedPost);
		Assert.Equal(inserted.Content, notifiedPost.Content);
		Assert.Equal(SurrealEventType.Create, notifiedType);
	}

	[Fact(DisplayName = "kill", Timeout = 5000)]
	public async Task Kill()
	{
		var tcs = new TaskCompletionSource<(Post, SurrealEventType)>();
		var live = await _di.TextRpc.LiveAsync<Post>("post", (post, type) =>
		{
			tcs.SetResult((post, type));

		});
		await _di.TextRpc.KillAsync(live);
		var timeout = Task.Delay(2500);
		var t = await Task.WhenAny(tcs.Task, timeout);
		Assert.False(t == tcs.Task);
		Assert.True(t == timeout);
		Assert.Null(t.Exception);
		Assert.Null(timeout.Exception);
	}

	[Fact(DisplayName = "query with the ONLY clause on record that exists", Timeout = 5000)]
	public async Task QuerySingleOnlyExists()
	{
		var content = "Querying a single record is simple!";
		var id = new Thing
		{
			Table = "post",
			Id = nameof(QuerySingleOnlyExists),
		};
		var created = await _di.TextRpc.CreateAsync(id, new Post(content));
		Assert.NotNull(created);
		Assert.Equal(id, created.Id);
		var result = await _di.TextRpc.QueryAsync("SELECT * FROM ONLY type::thing($id)", new { id = created.Id });
		var post = result.Get<Post>();
		Assert.NotNull(post);
		Assert.Equal(created.Id, post.Id);
		Assert.Equal(created.Content, post.Content);
	}

	[Fact(DisplayName = "query with the ONLY clause on non-existent record should throw an exception", Timeout = 5000)]
	public async Task QuerySingleOnlyNotFound()
	{
		var result = await _di.TextRpc.QueryAsync("SELECT * FROM ONLY type::thing($id)", new
		{
			id = new Thing
			{
				Table = "post",
				Id = "NotFound",
			}

		});

		var ex = Assert.ThrowsAny<Exception>(() => result.Get<Post>());
		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "query on no results should yield an empty collection", Timeout = 5000)]
	public async Task QueryOnNone()
	{
		var result = await _di.TextRpc.QueryAsync("SELECT * FROM type::thing($id)", new
		{
			id = new Thing
			{
				Table = "post",
				Id = nameof(QueryOnNone),
			}
		});

		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.Empty(posts);
	}

	[Fact(DisplayName = "query on many records should return them", Timeout = 5000)]
	public async Task QueryMultipleWithManyResults()
	{
		var inserted = await _di.TextRpc.InsertAsync("post", new Post[]
		{
			new(nameof(QueryMultipleWithManyResults) + "1"),
			new(nameof(QueryMultipleWithManyResults) + "2"),

		});
		var result = await _di.TextRpc.QueryAsync("SELECT * FROM post");
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.True(posts.Count() > 1);
	}

	[Fact(DisplayName = "query multiple on empty should return empty collection", Timeout = 5000)]
	public async Task QueryMultipleOnEmpty()
	{
		var result = await _di.TextRpc.QueryAsync("SELECT * FROM post WHERE content == $p", new
		{
			p = "NotFound"

		});
		var posts = result.Get<IEnumerable<Post>>();
		Assert.NotNull(posts);
		Assert.Empty(posts);
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
		var created = await _di.TextRpc.CreateAsync(id, new Post(content));
		Assert.NotNull(created);
		var result = await _di.TextRpc.SelectAsync<Post>(id);
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
		var result = await _di.TextRpc.SelectAsync<Post>(id);
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
		var created = await _di.TextRpc.CreateAsync(manual, new Post("Creating with manual id"));
		Assert.NotNull(created);
	}

	[Fact(DisplayName = "insert single record", Timeout = 5000)]
	public async Task InsertOne()
	{
		var content = "SurrealDB is pretty cool";
		var inserted = await _di.TextRpc.InsertAsync("post", new Post(content));
		Assert.NotNull(inserted);
		Assert.Equal(content, inserted.Content);
	}

	[Fact(DisplayName = "insert multiple records", Timeout = 5000)]
	public async Task InsertMany()
	{
		var content = "SurrealDB is pretty cool";
		var inserted = await _di.TextRpc.InsertAsync("post", new Post[]
		{
			new(content),
			new(content),

		});

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

		var created = await _di.TextRpc.CreateAsync(id, post);
		var updated = await _di.TextRpc.UpdateAsync(id, created with
		{
			Content = "New content!"

		});

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
		var upserted = await _di.TextRpc.UpdateAsync(id, post);

		Assert.Equal("Upsert this!", upserted.Content);
	}

	[Fact(DisplayName = "merge record that exists should update the record", Timeout = 5000)]
	public async Task MergeIntoExisting()
	{
		var id = new Thing
		{
			Table = "post",
			Id = nameof(MergeIntoExisting),
		};

		var created = await _di.TextRpc.CreateAsync(id, new Post("This post will be merged later"));
		Assert.NotNull(created);
		const string tags = "surrealdb,merge,tests";
		var merged = await _di.TextRpc.MergeAsync<Post>(id, new
		{
			tags

		});
		Assert.NotNull(merged);
		Assert.Equal(tags, merged.Tags);
	}

	[Fact(DisplayName = "merge record that does not exist should upsert the record", Timeout = 5000)]
	public async Task MergeIntoNonExistent()
	{
		var id = new Thing
		{
			Table = "post",
			Id = nameof(MergeIntoNonExistent),
		};

		const string tags = "surrealdb,merge,tests";
		var merged = await _di.TextRpc.MergeAsync<Post>(id, new
		{
			content = "redacted",
			tags

		});
		Assert.NotNull(merged);
		Assert.Equal(tags, merged.Tags);
	}

	[Fact(DisplayName = "merge table should update all records", Timeout = 5000)]
	public async Task MergeIntoTable()
	{
		var ids = Enumerable.Range(1, 3).Select(i => new Thing
		{
			Table = "post",
			Id = $"{nameof(MergeIntoTable)}{i}",
		});

		foreach (var id in ids)
		{
			var created = await _di.TextRpc.CreateAsync(id, new Post($"Post #{id.Id}"));
			Assert.NotNull(created);
		}

		const string tags = "surrealdb,merge,tests";
		var merged = await _di.TextRpc.BulkMergeAsync<Post>("post", new
		{
			tags

		});

		Assert.NotEmpty(merged);

		foreach (var m in merged)
			Assert.Equal(tags, m.Tags);
	}

	[Fact(DisplayName = "patch add + test ok", Timeout = 5000)]
	public async Task PatchAddTestOk()
	{
		const string content = "This is content";
		const string tags = "tags";
		Thing id = $"post:{nameof(PatchAddTestOk)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content));
		Assert.NotNull(created);

		await _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Add(p => p.Tags, tags)
			.Test(p => p.Content, content))
			;

		var post = await _di.TextRpc.SelectAsync<Post>(id);
		Assert.NotNull(post);

		Assert.Equal(content, post.Content);
		Assert.Equal(tags, post.Tags);
	}

	[Fact(DisplayName = "patch add + test fail", Timeout = 5000)]
	public async Task PatchAddTestFailed()
	{
		const string content = "This is content";
		const string tags = "tags";
		Thing id = $"post:{nameof(PatchAddTestFailed)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content));
		Assert.NotNull(created);

		var ex = await Assert.ThrowsAnyAsync<Exception>(() => _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Add(p => p.Tags, tags)
			.Test(p => p.Content, content + "nope")))
			;

		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "patch remove + test ok", Timeout = 5000)]
	public async Task PatchRemoveTestOk()
	{
		const string content = "This is content";
		const string tags = "tags";
		Thing id = $"post:{nameof(PatchRemoveTestOk)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content)
		{
			Tags = tags

		});
		Assert.NotNull(created);

		await _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Remove(p => p.Tags)
			.Test(p => p.Content, content))
			;

		var post = await _di.TextRpc.SelectAsync<Post>(id);
		Assert.NotNull(post);

		Assert.Equal(content, post.Content);
		Assert.Null(post.Tags);
	}

	[Fact(DisplayName = "patch remove + test fail", Timeout = 5000)]
	public async Task PatchRemoveTestFailed()
	{
		const string content = "This is content";
		const string tags = "tags";
		Thing id = $"post:{nameof(PatchRemoveTestFailed)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content)
		{
			Tags = tags

		});
		Assert.NotNull(created);

		var ex = await Assert.ThrowsAnyAsync<Exception>(() => _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Remove(p => p.Tags)
			.Test(p => p.Content, content + "nope")));

		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "patch replace + test ok", Timeout = 5000)]
	public async Task PatchReplaceTestOk()
	{
		const string content = "This is content";
		const string oldTags = "old tags";
		const string newTags = "new tags";
		Thing id = $"post:{nameof(PatchReplaceTestOk)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content)
		{
			Tags = oldTags

		});
		Assert.NotNull(created);

		await _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Replace(p => p.Tags, newTags)
			.Test(p => p.Content, content));

		var post = await _di.TextRpc.SelectAsync<Post>(id);
		Assert.NotNull(post);

		Assert.Equal(content, post.Content);
		Assert.Equal(newTags, post.Tags);
	}

	[Fact(DisplayName = "patch replace + test fail", Timeout = 5000)]
	public async Task PatchReplaceTestFailed()
	{
		const string content = "This is content";
		const string oldTags = "old tags";
		const string newTags = "new tags";
		Thing id = $"post:{nameof(PatchReplaceTestFailed)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content)
		{
			Tags = oldTags

		});
		Assert.NotNull(created);

		var ex = await Assert.ThrowsAnyAsync<Exception>(() => _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Replace(p => p.Tags, newTags)
			.Test(p => p.Content, content + "nope")));

		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "patch copy + test ok", Timeout = 5000)]
	public async Task PatchCopyTestOk()
	{
		const string content = "test,tags,patch";
		Thing id = $"post:{nameof(PatchCopyTestOk)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content));
		Assert.NotNull(created);

		await _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Copy(p => p.Content, p => p.Tags)
			.Test(p => p.Content, content))
			;

		var post = await _di.TextRpc.SelectAsync<Post>(id);
		Assert.NotNull(post);

		Assert.Equal(content, post.Content);
		Assert.Equal(content, post.Tags);
	}

	[Fact(DisplayName = "patch copy + test fail", Timeout = 5000)]
	public async Task PatchCopyTestFailed()
	{
		const string content = "test,tags,patch";
		Thing id = $"post:{nameof(PatchCopyTestFailed)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content));
		Assert.NotNull(created);

		var ex = await Assert.ThrowsAnyAsync<Exception>(() => _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Copy(p => p.Content, p => p.Tags)
			.Test(p => p.Content, content + "nope")));

		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "patch move + test ok", Timeout = 5000)]
	public async Task PatchMoveTestOk()
	{
		const string content = "Woopsie";
		const string tags = "This is actually content";
		Thing id = $"post:{nameof(PatchMoveTestOk)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content) { Tags = tags });
		Assert.NotNull(created);

		await _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Test(p => p.Content, content)
			.Move(p => p.Tags, p => p.Content));

		var post = await _di.TextRpc.SelectAsync<Post>(id);
		Assert.NotNull(post);

		Assert.Equal(tags, post.Content);
		Assert.Null(post.Tags);
	}

	[Fact(DisplayName = "patch move + test fail", Timeout = 5000)]
	public async Task PatchMoveTestFailed()
	{
		const string content = "Woopsie";
		const string tags = "This is actually content";
		Thing id = $"post:{nameof(PatchMoveTestFailed)}";
		var created = await _di.TextRpc.CreateAsync(id, new Post(content) { Tags = tags });
		Assert.NotNull(created);

		var ex = await Assert.ThrowsAnyAsync<Exception>(() => _di.TextRpc.PatchAsync<Post>(id, patch => patch
			.Move(p => p.Tags, p => p.Content)
			.Test(p => p.Content, content + "nope")));

		Assert.True(ex is SurrealException || ex is AggregateException agg && agg.InnerExceptions.All(e => e is SurrealException));
	}

	[Fact(DisplayName = "delete existing", Timeout = 5000)]
	public async Task DeleteExistingRecord()
	{
		Thing id = $"post:{nameof(DeleteExistingRecord)}";
		var content = "This will be deleted";
		var existing = await _di.TextRpc.CreateAsync(id, new Post("This will be deleted"));

		var deleted = await _di.TextRpc.DeleteAsync<Post>(id);

		Assert.Equal(content, deleted?.Content);
	}

	[Fact(DisplayName = "delete not found", Timeout = 5000)]
	public async Task DeleteNotFound()
	{
		Thing id = $"post:{nameof(DeleteNotFound)}";

		var deleted = await _di.TextRpc.DeleteAsync<Post>(id);

		Assert.Null(deleted);
	}
}
