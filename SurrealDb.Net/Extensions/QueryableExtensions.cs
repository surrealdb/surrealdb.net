using System.Linq.Expressions;
using System.Reflection;
using SurrealDb.Net.Internals.Queryable;

namespace SurrealDb.Net;

// 💡 Inspired by https://github.com/dotnet/efcore/blob/f96570aecfc93fe49fbaa5f1f9515b3a3f3c038e/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs

public static class QueryableExtensions
{
    /// <summary>
    ///     Generates a string representation of the query used. This string may not be suitable for direct execution and is intended only
    ///     for use in debugging.
    /// </summary>
    /// <param name="source">The query source.</param>
    /// <returns>The query string for debugging.</returns>
    public static string ToQueryString(this IQueryable source)
    {
        if (source.Provider is ISurrealDbQueryProvider surrealDbQueryProvider)
        {
            var (query, _) = surrealDbQueryProvider.Translate(source.Expression);
            return query;
        }

        return "The given 'IQueryable' does not support generation of query strings.";
    }

    #region AsAsyncEnumerable

    /// <summary>
    ///     Returns an <see cref="IAsyncEnumerable{T}" /> which can be enumerated asynchronously.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to enumerate.</param>
    /// <returns>The query results.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> is not a <see cref="IAsyncEnumerable{T}" />.</exception>
    public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(
        this IQueryable<TSource> source
    )
    {
        if (source is IAsyncEnumerable<TSource> asyncEnumerable)
        {
            return asyncEnumerable;
        }

        throw new InvalidOperationException("This IQueryable does not handle async");
    }

    #endregion

    #region Any/All

    /// <summary>
    ///     Asynchronously determines whether a sequence contains any elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to check for being empty.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="true" /> if the source sequence contains any elements; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<bool> AnyAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Any))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<bool>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously determines whether any element of a sequence satisfies a condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> whose elements to test for a condition.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="true" /> if any elements in the source sequence pass the test in the specified
    ///     predicate; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<bool> AnyAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Any))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<bool>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously determines whether all the elements of a sequence satisfy a condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> whose elements to test for a condition.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="true" /> if every element of the source sequence passes the test in the specified
    ///     predicate; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<bool> AllAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethod(nameof(Queryable.All))
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<bool>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    #endregion

    #region Count/LongCount

    /// <summary>
    ///     Asynchronously returns the number of elements in a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to be counted.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the number of elements in the input sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<int> CountAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Count))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<int>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the number of elements in a sequence that satisfy a condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to be counted.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the number of elements in the sequence that satisfy the condition in the predicate function.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<int> CountAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Count))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<int>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns a <see cref="long" /> that represents the total number of elements in a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to be counted.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the number of elements in the input sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<long> LongCountAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.LongCount))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<long>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns a <see cref="long" /> that represents the number of elements in a sequence
    ///     that satisfy a condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to be counted.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the number of elements in the sequence that satisfy the condition in the predicate function.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<long> LongCountAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.LongCount))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<long>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    #endregion

    #region ElementAt/ElementAtOrDefault

    /// <summary>
    ///     Asynchronously returns the element at a specified index in a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the element from.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the element at a specified index in a <paramref name="source" /> sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para>
    ///         <paramref name="index" /> is less than zero.
    ///     </para>
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> ElementAtAsync<TSource>(
        this IQueryable<TSource> source,
        int index,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethod(nameof(Queryable.ElementAt))
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, [source.Expression, Expression.Constant(index)]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the element at a specified index in a sequence, or a default value if the index is out of range.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the element from.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the element at a specified index in a <paramref name="source" /> sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> ElementAtOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        int index,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethod(nameof(Queryable.ElementAtOrDefault))
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, [source.Expression, Expression.Constant(index)]),
            cancellationToken
        );
    }

    #endregion

    #region First/FirstOrDefault

    /// <summary>
    ///     Asynchronously returns the first element of a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the first element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the first element in <paramref name="source" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> FirstAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.First))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the first element of a sequence that satisfies a specified condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the first element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the first element in <paramref name="source" /> that passes the test in
    ///     <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para>
    ///         No element satisfies the condition in <paramref name="predicate" />
    ///     </para>
    ///     <para>
    ///         -or -
    ///     </para>
    ///     <para>
    ///         <paramref name="source" /> contains no elements.
    ///     </para>
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> FirstAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.First))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the first element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if
    ///     <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.FirstOrDefault))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the first element of a sequence that satisfies a specified condition
    ///     or a default value if no such element is found.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the first element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if <paramref name="source" />
    ///     is empty or if no element passes the test specified by <paramref name="predicate" />, otherwise, the first
    ///     element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.FirstOrDefault))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, bool>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    #endregion

    #region Last/LastOrDefault

    /// <summary>
    ///     Asynchronously returns the last element of a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the last element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the last element in <paramref name="source" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> LastAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Last))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the last element of a sequence that satisfies a specified condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the last element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the last element in <paramref name="source" /> that passes the test in
    ///     <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para>
    ///         No element satisfies the condition in <paramref name="predicate" />.
    ///     </para>
    ///     <para>
    ///         -or-
    ///     </para>
    ///     <para>
    ///         <paramref name="source" /> contains no elements.
    ///     </para>
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> LastAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Last))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the last element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the last element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if
    ///     <paramref name="source" /> is empty; otherwise, the last element in <paramref name="source" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> LastOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.LastOrDefault))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the last element of a sequence that satisfies a specified condition
    ///     or a default value if no such element is found.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the last element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if <paramref name="source" />
    ///     is empty or if no element passes the test specified by <paramref name="predicate" />, otherwise, the last
    ///     element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> LastOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.LastOrDefault))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, bool>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    #endregion

    #region Single/SingleOrDefault

    /// <summary>
    ///     Asynchronously returns the only element of a sequence, and throws an exception
    ///     if there is not exactly one element in the sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the single element of the input sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    ///     <para>
    ///         <paramref name="source" /> contains more than one elements.
    ///     </para>
    ///     <para>
    ///         -or-
    ///     </para>
    ///     <para>
    ///         <paramref name="source" /> contains no elements.
    ///     </para>
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> SingleAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Single))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the only element of a sequence that satisfies a specified condition,
    ///     and throws an exception if more than one such element exists.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the single element of the input sequence that satisfies the condition in
    ///     <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para>
    ///         No element satisfies the condition in <paramref name="predicate" />.
    ///     </para>
    ///     <para>
    ///         -or-
    ///     </para>
    ///     <para>
    ///         More than one element satisfies the condition in <paramref name="predicate" />.
    ///     </para>
    ///     <para>
    ///         -or-
    ///     </para>
    ///     <para>
    ///         <paramref name="source" /> contains no elements.
    ///     </para>
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> SingleAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Single))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the only element of a sequence, or a default value if the sequence is empty;
    ///     this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the single element of the input sequence, or <see langword="default" /> (
    ///     <typeparamref name="TSource" />)
    ///     if the sequence contains no elements.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains more than one element.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.SingleOrDefault))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously returns the only element of a sequence that satisfies a specified condition or
    ///     a default value if no such element exists; this method throws an exception if more than one element
    ///     satisfies the condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the single element of the input sequence that satisfies the condition in
    ///     <paramref name="predicate" />, or <see langword="default" /> ( <typeparamref name="TSource" /> ) if no such element is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     More than one element satisfies the condition in <paramref name="predicate" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.SingleOrDefault))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, bool>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource?>(
            Expression.Call(methodInfo, [source.Expression, predicate]),
            cancellationToken
        );
    }

    #endregion

    #region Min

    /// <summary>
    ///     Asynchronously returns the minimum value of a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to determine the minimum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the minimum value in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> MinAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Min))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously invokes a projection function on each element of a sequence and returns the minimum resulting value.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by the function represented by <paramref name="selector" />.
    /// </typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to determine the minimum of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the minimum value in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TResult> MinAsync<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Min))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, TResult>>);
            })
            .MakeGenericMethod(typeof(TSource), typeof(TResult));

        return provider.ExecuteAsync<TResult>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    #endregion

    #region Max

    /// <summary>
    ///     Asynchronously returns the maximum value of a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to determine the maximum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the maximum value in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TSource> MaxAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Max))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<TSource>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously invokes a projection function on each element of a sequence and returns the maximum resulting value.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by the function represented by <paramref name="selector" />.
    /// </typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> that contains the elements to determine the maximum of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the maximum value in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<TResult> MaxAsync<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Max))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, TResult>>);
            })
            .MakeGenericMethod(typeof(TSource), typeof(TResult));

        return provider.ExecuteAsync<TResult>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    #endregion

    #region Sum

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal> SumAsync(
        this IQueryable<decimal> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(decimal);
            });

        return provider.ExecuteAsync<decimal>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal?> SumAsync(
        this IQueryable<decimal?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(decimal?);
            });

        return provider.ExecuteAsync<decimal?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, decimal>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, decimal>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<decimal>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal?> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, decimal?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, decimal?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<decimal?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<int> SumAsync(
        this IQueryable<int> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(int);
            });

        return provider.ExecuteAsync<int>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<int?> SumAsync(
        this IQueryable<int?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(int?);
            });

        return provider.ExecuteAsync<int?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<int> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, int>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, int>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<int>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<int?> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, int?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, int?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<int?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<long> SumAsync(
        this IQueryable<long> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(long);
            });

        return provider.ExecuteAsync<long>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<long?> SumAsync(
        this IQueryable<long?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(long?);
            });

        return provider.ExecuteAsync<long?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<long> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, long>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, long>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<long>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<long?> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, long?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, long?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<long?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> SumAsync(
        this IQueryable<double> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(double);
            });

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> SumAsync(
        this IQueryable<double?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(double?);
            });

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, double>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, double>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, double?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, double?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float> SumAsync(
        this IQueryable<float> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(float);
            });

        return provider.ExecuteAsync<float>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the values in the sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float?> SumAsync(
        this IQueryable<float?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(float?);
            });

        return provider.ExecuteAsync<float?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, float>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, float>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<float>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
    ///     each element of the input sequence.
    /// </summary>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the sum of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float?> SumAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, float?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Sum))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, float?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<float?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    #endregion

    #region Average

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal> AverageAsync(
        this IQueryable<decimal> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(decimal);
            });

        return provider.ExecuteAsync<decimal>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal?> AverageAsync(
        this IQueryable<decimal?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(decimal?);
            });

        return provider.ExecuteAsync<decimal?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, decimal>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, decimal>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<decimal>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<decimal?> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, decimal?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, decimal?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<decimal?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> AverageAsync(
        this IQueryable<int> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(int);
            });

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> AverageAsync(
        this IQueryable<int?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(int?);
            });

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, int>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, int>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, int?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, int?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> AverageAsync(
        this IQueryable<long> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(long);
            });

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> AverageAsync(
        this IQueryable<long?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(long?);
            });

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, long>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, long>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, long?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, long?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> AverageAsync(
        this IQueryable<double> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(double);
            });

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> AverageAsync(
        this IQueryable<double?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(double?);
            });

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, double>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, double>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<double?> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, double?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, double?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<double?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float> AverageAsync(
        this IQueryable<float> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(float);
            });

        return provider.ExecuteAsync<float>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the sequence of values.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float?> AverageAsync(
        this IQueryable<float?> source,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(float?);
            });

        return provider.ExecuteAsync<float?>(
            Expression.Call(methodInfo, source.Expression),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, float>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, float>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<float>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    /// <summary>
    ///     Asynchronously computes the average of a sequence of values that is obtained
    ///     by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the average of the projected values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<float?> AverageAsync<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, float?>> selector,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#endif

        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Average))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[1].ParameterType == typeof(Expression<Func<TSource, float?>>);
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<float?>(
            Expression.Call(methodInfo, [source.Expression, selector]),
            cancellationToken
        );
    }

    #endregion

    #region Contains

    /// <summary>
    ///     Asynchronously determines whether a sequence contains a specified element by using the default equality comparer.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to return the single element of.</param>
    /// <param name="item">The object to locate in the sequence.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="true" /> if the input sequence contains the specified value; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static Task<bool> ContainsAsync<TSource>(
        this IQueryable<TSource> source,
        TSource item,
        CancellationToken cancellationToken = default
    )
    {
        var provider = ExtractAsyncQueryProvider(source);
        var methodInfo = GetQueryableMethods(nameof(Queryable.Contains))
            .First(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2;
            })
            .MakeGenericMethod(typeof(TSource));

        return provider.ExecuteAsync<bool>(
            Expression.Call(
                methodInfo,
                [source.Expression, Expression.Constant(item, typeof(TSource))]
            ),
            cancellationToken
        );
    }

    #endregion

    #region ToList/Array

    /// <summary>
    ///     Asynchronously creates a <see cref="List{T}" /> from an <see cref="IQueryable{T}" /> by enumerating it asynchronously.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to create a list from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains a <see cref="List{T}" /> that contains elements from the input sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static async Task<List<TSource>> ToListAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<TSource>();
        await foreach (
            var element in source
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            list.Add(element);
        }

        return list;
    }

    /// <summary>
    ///     Asynchronously creates an array from an <see cref="IQueryable{T}" /> by enumerating it asynchronously.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to create an array from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains an array that contains elements from the input sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static async Task<TSource[]> ToArrayAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    ) => (await source.ToListAsync(cancellationToken).ConfigureAwait(false)).ToArray();

    #endregion

    #region ForEach

    /// <summary>
    ///     Asynchronously enumerates the query results and performs the specified action on each element.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}" /> to enumerate.</param>
    /// <param name="action">The action to perform on each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="action" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public static async Task ForEachAsync<T>(
        this IQueryable<T> source,
        Action<T> action,
        CancellationToken cancellationToken = default
    )
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(action);
#endif

        await foreach (
            var element in source
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            action(element);
        }
    }

    #endregion

    #region Impl.

    private static IAsyncQueryProvider ExtractAsyncQueryProvider<TSource>(
        this IQueryable<TSource> source
    )
    {
        return source.Provider as IAsyncQueryProvider
            ?? throw new InvalidOperationException(
                "The provider for the source 'IQueryable' doesn't implement 'IAsyncQueryProvider'. Only providers that implement 'IAsyncQueryProvider' can be used for asynchronous operations."
            );
    }

    private static MethodInfo GetQueryableMethod(string methodName)
    {
        return typeof(Queryable)
            .GetMethods()
            .First(method => string.Equals(method.Name, methodName, StringComparison.Ordinal));
    }

    private static IEnumerable<MethodInfo> GetQueryableMethods(string methodName)
    {
        return typeof(Queryable)
            .GetMethods()
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal));
    }

    #endregion
}
