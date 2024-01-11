using System.Linq.Expressions;
using System.Reflection;

namespace SurrealDb.Net.Tests.Queryable;

public class AggregatingQueryableTests : BaseQueryableTests
{
    [Test]
    public void Count()
    {
        string query = ToSurql(Expression.Call(CreateCountMethod<Post>(1), Posts.Expression));

        query
            .Should()
            .Be(
                """
                (SELECT count() FROM post GROUP ALL)[0].count
                """
            );
    }

    [Test]
    public void CountWithPredicate()
    {
        string query = ToSurql(
            Expression.Call(
                CreateCountMethod<Post>(2),
                Posts.Expression,
                (Post post) => post.Status == "DRAFT"
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT count(status == "DRAFT") FROM post GROUP ALL)[0].count
                """
            );
    }

    [Test]
    public void Sum()
    {
        string query = ToSurql(
            Expression.Call(CreateSumMethod(1), Users.Select(u => u.Age).Expression)
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::sum(Age) AS Sum FROM user GROUP ALL)[0].Sum
                """
            );
    }

    [Test]
    public void SumWithSelector()
    {
        string query = ToSurql(
            Expression.Call(
                CreateSumMethod<Models.User>(2),
                Users.Expression,
                (Models.User user) => user.Age
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::sum(Age) AS Sum FROM user GROUP ALL)[0].Sum
                """
            );
    }

    [Test]
    public void Min()
    {
        string query = ToSurql(
            Expression.Call(CreateMinMethod<int>(1), Users.Select(u => u.Age).Expression)
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::min(Age) AS Min FROM user GROUP ALL)[0].Min
                """
            );
    }

    [Test]
    public void MinWithSelector()
    {
        string query = ToSurql(
            Expression.Call(
                CreateMinMethod<Models.User, int>(2),
                Users.Expression,
                (Models.User user) => user.Age
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::min(Age) AS Min FROM user GROUP ALL)[0].Min
                """
            );
    }

    [Test]
    public void Max()
    {
        string query = ToSurql(
            Expression.Call(CreateMaxMethod<int>(1), Users.Select(u => u.Age).Expression)
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::max(Age) AS Max FROM user GROUP ALL)[0].Max
                """
            );
    }

    [Test]
    public void MaxWithSelector()
    {
        string query = ToSurql(
            Expression.Call(
                CreateMaxMethod<Models.User, int>(2),
                Users.Expression,
                (Models.User user) => user.Age
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::max(Age) AS Max FROM user GROUP ALL)[0].Max
                """
            );
    }

    [Test]
    public void Average()
    {
        string query = ToSurql(
            Expression.Call(CreateAverageMethod(1), Users.Select(u => u.Age).Expression)
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::mean(Age) AS Avg FROM user GROUP ALL)[0].Avg
                """
            );
    }

    [Test]
    public void AverageWithSelector()
    {
        string query = ToSurql(
            Expression.Call(
                CreateAverageMethod<Models.User>(2),
                Users.Expression,
                (Models.User user) => user.Age
            )
        );

        query
            .Should()
            .Be(
                """
                (SELECT math::mean(Age) AS Avg FROM user GROUP ALL)[0].Avg
                """
            );
    }

    private static MethodInfo CreateCountMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Count), argsLength);
    }

    private static MethodInfo CreateSumMethod(int argsLength)
    {
        return CreateQueryableMethod(nameof(System.Linq.Queryable.Sum), argsLength);
    }

    private static MethodInfo CreateSumMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Sum), argsLength);
    }

    private static MethodInfo CreateMinMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Min), argsLength);
    }

    private static MethodInfo CreateMinMethod<TSource, TResult>(int argsLength)
    {
        return CreateQueryableMethod<TSource, TResult>(
            nameof(System.Linq.Queryable.Min),
            argsLength
        );
    }

    private static MethodInfo CreateMaxMethod<T>(int argsLength)
    {
        return CreateQueryableMethod<T>(nameof(System.Linq.Queryable.Max), argsLength);
    }

    private static MethodInfo CreateMaxMethod<TSource, TResult>(int argsLength)
    {
        return CreateQueryableMethod<TSource, TResult>(
            nameof(System.Linq.Queryable.Max),
            argsLength
        );
    }

    private static MethodInfo CreateAverageMethod(int argsLength)
    {
        return CreateQueryableMethod(nameof(System.Linq.Queryable.Average), argsLength);
    }

    private static MethodInfo CreateAverageMethod<TSource>(int argsLength)
    {
        return CreateQueryableMethod<TSource>(nameof(System.Linq.Queryable.Average), argsLength);
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
