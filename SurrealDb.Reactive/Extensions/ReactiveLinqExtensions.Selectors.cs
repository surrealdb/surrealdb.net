using SurrealDb.Net.Models;
using SurrealDb.Net.Models.LiveQuery;

namespace System.Reactive.Linq;

public static partial class ReactiveLinqExtensions
{
    /// <summary>
    /// Selects result responses from a live query (all actions CREATE, UPDATE and DELETE, except CLOSE).
    /// </summary>
    /// <param name="source">An observable sequence to filter.</param>
    public static IObservable<SurrealDbLiveQueryResponse> SelectResults(
        this IObservable<SurrealDbLiveQueryResponse> source
    )
    {
        return source.Where(x => x is not SurrealDbLiveQueryCloseResponse);
    }

    /// <summary>
    /// Selects created records from live query notifications.
    /// </summary>
    /// <typeparam name="T">The type of record created.</typeparam>
    /// <param name="source">An observable sequence to filter.</param>
    public static IObservable<T> SelectCreatedRecords<T>(
        this IObservable<SurrealDbLiveQueryResponse> source
    )
    {
        return source.OfType<SurrealDbLiveQueryCreateResponse<T>>().Select(x => x.Result);
    }

    /// <summary>
    /// Selects updated records from live query notifications.
    /// </summary>
    /// <typeparam name="T">The type of record updated.</typeparam>
    /// <param name="source">An observable sequence to filter.</param>
    public static IObservable<T> SelectUpdatedRecords<T>(
        this IObservable<SurrealDbLiveQueryResponse> source
    )
    {
        return source.OfType<SurrealDbLiveQueryUpdateResponse<T>>().Select(x => x.Result);
    }

    /// <summary>
    /// Selects deleted record ids from live query notifications.
    /// </summary>
    /// <param name="source">An observable sequence to filter.</param>
    public static IObservable<Thing> SelectDeletedIds(
        this IObservable<SurrealDbLiveQueryResponse> source
    )
    {
        return source.OfType<SurrealDbLiveQueryDeleteResponse>().Select(x => x.Result);
    }
}
