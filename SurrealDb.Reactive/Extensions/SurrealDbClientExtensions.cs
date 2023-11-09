﻿using System.Reactive.Linq;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net;

public static class SurrealDbClientExtensions
{
    /// <summary>
    /// Initiates a live query from a SurrealQL query.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the live query.</typeparam>
    /// <param name="client">A <see cref="ISurrealDbClient"/> instance.</param>
    /// <param name="query">The SurrealQL query that initiates a Live Query, must be of the form "LIVE SELECT * FROM table;".</param>
    /// <param name="parameters">A list of parameters to be used inside the SurrealQL query.</param>
    /// <returns>Returns an Observable to consume incoming live query notification.</returns>
    public static IObservable<SurrealDbLiveQueryResponse> ObserveQuery<T>(
        this ISurrealDbClient client,
        string query,
        IReadOnlyDictionary<string, object>? parameters = null
    )
    {
        return Observable.Create<SurrealDbLiveQueryResponse>(
            async (observer, cancellationToken) =>
            {
                SurrealDbResponse response = null!;

                try
                {
                    response = await client.Query(query, parameters, cancellationToken);
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                if (response.HasErrors)
                {
                    observer.OnError(new SurrealDbErrorResultException(response.FirstError!));
                    return () => { };
                }

                if (response.FirstOk is null)
                {
                    observer.OnError(new SurrealDbErrorResultException());
                    return () => { };
                }

                // TODO : handle multi-queries

                SurrealDbLiveQuery<T> liveQuery = null!;

                var queryUuid = response.FirstOk!.GetValue<Guid>();

                try
                {
                    liveQuery = client.ListenLive<T>(queryUuid);
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                    return () => { };
                }

                var subscription = liveQuery.ToObservable().Subscribe(observer);

                return () =>
                {
                    subscription.Dispose();
                    liveQuery.DisposeAsync().GetAwaiter().GetResult();
                };
            }
        );
    }

    /// <summary>
    /// Initiates a live query on a table.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the live query.</typeparam>
    /// <param name="client">A <see cref="ISurrealDbClient"/> instance.</param>
    /// <param name="table">The name of the database table to watch.</param>
    /// <param name="diff">
    /// If set to true, live notifications will include an array of JSON Patch objects,
    /// rather than the entire record for each notification.
    /// </param>
    /// <returns>Returns an Observable to consume incoming live query notification.</returns>
    public static IObservable<SurrealDbLiveQueryResponse> ObserveTable<T>(
        this ISurrealDbClient client,
        string table,
        bool diff = false
    )
    {
        return Observable.Create<SurrealDbLiveQueryResponse>(
            async (observer, cancellationToken) =>
            {
                SurrealDbLiveQuery<T> liveQuery = null!;

                try
                {
                    liveQuery = await client.LiveTable<T>(table, diff, cancellationToken);
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                    return () => { };
                }

                var subscription = liveQuery.ToObservable().Subscribe(observer);

                return () =>
                {
                    subscription.Dispose();
                    liveQuery.DisposeAsync().GetAwaiter().GetResult();
                };
            }
        );
    }
}
