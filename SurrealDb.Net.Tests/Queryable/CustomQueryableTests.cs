using System.Linq.Expressions;
using System.Reflection;

namespace SurrealDb.Net.Tests.Queryable;

public class CustomQueryableTests : BaseQueryableTests
{
    [Test]
    public void All()
    {
        string query = ToSurql(
            Expression.Call(
                CreateAllMethod<Models.User>(2),
                Users.Expression,
                (Models.User user) => user.Age >= 18
            )
        );

        query
            .Should()
            .Be(
                """
                array::len((SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user WHERE !(Age >= 18))) == 0
                """
            );
    }

    [Test]
    public void Any()
    {
        string query = ToSurql(Expression.Call(CreateAnyMethod<Post>(1), Posts.Expression));

        query
            .Should()
            .Be(
                """
                array::len((SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post)) > 0
                """
            );
    }

    [Test]
    public void Contains()
    {
        string query = ToSurql(
            Expression.Call(
                CreateContainsMethod<string?>(2),
                Posts.Select(p => p.Status).Expression,
                Expression.Constant("DRAFT")
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT VALUE status FROM post) CONTAINS "DRAFT"
                """
            );
    }

    [Test]
    public void Distinct()
    {
        string query = ToSurql(Posts.Select(p => p.Status).Distinct());

        query
            .Should()
            .Be(
                """
                (SELECT array::distinct(status) AS Values FROM post GROUP ALL)[0].Values
                """
            );
    }

    [Test]
    public void ElementAt()
    {
        string query = ToSurql(
            Expression.Call(
                CreateElementAtMethod<string?>(2),
                Posts.Select(p => p.Status).Expression,
                Expression.Constant(2)
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT VALUE status FROM post)[2]
                """
            );
    }

    [Test]
    public void ElementAtOrDefault()
    {
        string query = ToSurql(
            Expression.Call(
                CreateElementAtOrDefaultMethod<int>(2),
                Users.Select(p => p.Age).Expression,
                Expression.Constant(2)
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT VALUE Age FROM user)[2] ?? 0
                """
            );
    }

    [Test]
    public void First()
    {
        string query = ToSurql(
            Expression.Call(CreateFirstMethod<string?>(1), Posts.Select(p => p.Status).Expression)
        );

        query
            .Should()
            .Be(
                """
                (SELECT VALUE status FROM post)[0]
                """
            );
    }

    [Test]
    public void FirstOrDefault()
    {
        string query = ToSurql(
            Expression.Call(CreateFirstOrDefaultMethod<Models.User>(1), Users.Expression)
        );

        query
            .Should()
            .Be(
                """
                (SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user)[0] ?? null
                """
            );
    }

    [Test]
    public void Last()
    {
        string query = ToSurql(
            Expression.Call(CreateLastMethod<string?>(1), Posts.Select(p => p.Status).Expression)
        );

        query
            .Should()
            .Be(
                """
                array::last((SELECT VALUE status FROM post))
                """
            );
    }

    [Test]
    public void LastOrDefault()
    {
        string query = ToSurql(
            Expression.Call(CreateLastOrDefaultMethod<Models.User>(1), Users.Expression)
        );

        query
            .Should()
            .Be(
                """
                array::last((SELECT Age, id AS Id, IsActive, IsAdmin, IsOwner, Username FROM user)) ?? null
                """
            );
    }

    [Test]
    public void Single()
    {
        string query = ToSurql(Expression.Call(CreateSingleMethod<Post>(1), Posts.Expression));

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM ONLY post
                """
            );
    }

    private static MethodInfo CreateAllMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.All), argsLength);
    }

    private static MethodInfo CreateAnyMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Any), argsLength);
    }

    private static MethodInfo CreateContainsMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Contains), argsLength);
    }

    private static MethodInfo CreateElementAtMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.ElementAt), argsLength);
    }

    private static MethodInfo CreateElementAtOrDefaultMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(
            nameof(System.Linq.Queryable.ElementAtOrDefault),
            argsLength
        );
    }

    private static MethodInfo CreateFirstMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.First), argsLength);
    }

    private static MethodInfo CreateFirstOrDefaultMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.FirstOrDefault), argsLength);
    }

    private static MethodInfo CreateLastMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Last), argsLength);
    }

    private static MethodInfo CreateLastOrDefaultMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.LastOrDefault), argsLength);
    }

    private static MethodInfo CreateSingleMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Single), argsLength);
    }

    private static MethodInfo CreateQueryableMethod(string name, int argsLength)
    {
        return GetQueryableMethod(name, argsLength);
    }

    private static MethodInfo CreateQueryableMethod<T>(string name, int argsLength)
    {
        return GetQueryableMethod(name, argsLength).MakeGenericMethod(typeof(T));
    }

    private static MethodInfo CreateQueryableMethod<TSource, TResult>(string name, int argsLength)
    {
        return GetQueryableMethod(name, argsLength)
            .MakeGenericMethod(typeof(TSource), typeof(TResult));
    }

    private static MethodInfo GetQueryableMethod(string name, int argsLength)
    {
        return typeof(System.Linq.Queryable)
            .GetMethods()
            .Where(method => method.Name == name)
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == argsLength
                    && (parameters.Length < 2 || parameters[1].Name != "comparer");
            });
    }
}
