using SurrealDb.Net.Models;
using SurrealDb.Net.Models.LiveQuery;

namespace System.Reactive.Linq;

public static partial class ReactiveLinqExtensions
{
    /// <summary>
    /// Applies an accumulator function over an observable sequence, returning the result of the aggregation as a single element in the result sequence.
    /// The accumulator will handle CREATE, UPDATE and DELETE events to fill the provided <paramref name="seed"/>.
    /// The specified seed value is used as the initial accumulator value.
    /// For aggregation behavior with incremental intermediate results, see <see cref="ScanRecords{T}(IObservable{SurrealDbLiveQueryResponse}, IDictionary{string, T})"/>.
    /// </summary>
    /// <typeparam name="T">The record type inside each <see cref="SurrealDbLiveQueryResponse"/>.</typeparam>
    /// <param name="source">An observable sequence to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <returns>An observable sequence containing a single element with the final accumulator value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="seed"/> is null.</exception>
    public static IObservable<IDictionary<string, T>> AggregateRecords<T>(
        this IObservable<SurrealDbLiveQueryResponse> source,
        IDictionary<string, T> seed
    )
        where T : IRecord
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (seed is null)
        {
            throw new ArgumentNullException(nameof(seed));
        }

        return source.Aggregate(seed, GetRecordsAccumulator<T>());
    }

    /// <summary>
    /// Applies an accumulator function over an observable sequence and returns each intermediate result.
    /// The accumulator will handle CREATE, UPDATE and DELETE events to fill the provided <paramref name="seed"/>.
    /// For aggregation behavior with no intermediate results, see <see cref="AggregateRecords{T}(IObservable{SurrealDbLiveQueryResponse}, IDictionary{string, T})"/>.
    /// </summary>
    /// <typeparam name="T">The record type inside each <see cref="SurrealDbLiveQueryResponse"/>.</typeparam>
    /// <param name="source">An observable sequence to accumulate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <returns>An observable sequence containing the accumulated values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="seed"/> is null.</exception>
    public static IObservable<IDictionary<string, T>> ScanRecords<T>(
        this IObservable<SurrealDbLiveQueryResponse> source,
        IDictionary<string, T> seed
    )
        where T : IRecord
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (seed is null)
        {
            throw new ArgumentNullException(nameof(seed));
        }

        return source.Scan(seed, GetRecordsAccumulator<T>());
    }

    private static Func<
        IDictionary<string, T>,
        SurrealDbLiveQueryResponse,
        IDictionary<string, T>
    > GetRecordsAccumulator<T>()
        where T : IRecord
    {
        // TODO : This only works with RecordId of string. Find a way to make it work for any type of RecordId.
        return (acc, response) =>
        {
            if (
                response is SurrealDbLiveQueryCreateResponse<T> createResponse
                && createResponse.Result.Id is not null
            )
            {
                acc[createResponse.Result.Id.DeserializeId<string>()] = createResponse.Result;
                return acc;
            }

            if (
                response is SurrealDbLiveQueryUpdateResponse<T> updateResponse
                && updateResponse.Result.Id is not null
            )
            {
                acc[updateResponse.Result.Id.DeserializeId<string>()] = updateResponse.Result;
                return acc;
            }

            if (
                response is SurrealDbLiveQueryDeleteResponse<T> deleteResponse
                && deleteResponse.Result.Id is not null
            )
            {
                acc.Remove(deleteResponse.Result.Id.DeserializeId<string>());
                return acc;
            }

            return acc;
        };
    }
}
