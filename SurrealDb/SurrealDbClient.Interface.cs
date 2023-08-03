using SurrealDb.Models;
using SurrealDb.Models.Auth;
using SurrealDb.Models.Response;

namespace SurrealDb;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public interface ISurrealDbClient
{
    /// <summary>
    /// The uri linked to the SurrealDB instance target.
    /// </summary>
    Uri Uri { get; }

    /// <summary>
    /// Authenticates the current connection with a JWT.
    /// </summary>
    /// <param name="jwt">The JWT holder of the token.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task Authenticate(Jwt jwt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the SurrealDB instance. This can improve performance to avoid cold starts.<br /><br />
    /// 
    /// * Using HTTP(S) protocol: has no real effect, except to detect if a connection is possible or not 
    /// (throws exception if unable to connect)<br />
    /// 
    /// * Using WS(S) protocol: will start a websocket connection so that further calls will be triggered immediately
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task Connect(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the specific record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="data">The record to create.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<T> Create<T>(T data, CancellationToken cancellationToken = default) where T : Record;

    /// <summary>
    /// Creates a record in a table in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="table">The table name where the record will be stored.</param>
    /// <param name="data">The record to create.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<T> Create<T>(string table, T data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all records in a table from the database.
    /// </summary>
    /// <param name="table">The name of the database table</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task Delete(string table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified record from the database.
    /// </summary>
    /// <param name="table">The name of the database table</param>
    /// <param name="id">The id of the record</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns true if the record was removed successfully.</returns>
    Task<bool> Delete(string table, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified record from the database.
    /// </summary>
    /// <param name="thing">The record id.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns true if the record was removed successfully.</returns>
    Task<bool> Delete(Thing thing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the authentication for the current connection.
    /// </summary>
    void Invalidate();

	/// <summary>
	/// Modifies the specified record in the database.
	/// </summary>
	/// <typeparam name="TPatch">The type of the patch update.</typeparam>
	/// <typeparam name="TOutput">The type of the record updated.</typeparam>
	/// <param name="data">The record to patch.</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	/// <returns>The record updated.</returns>
	Task<TOutput> Patch<TPatch, TOutput>(TPatch data, CancellationToken cancellationToken = default) where TPatch : Record;

	/// <summary>
	/// Modifies the specified record in the database.
	/// </summary>
	/// <typeparam name="T">The type of the record updated.</typeparam>
	/// <param name="thing">The record id.</param>
	/// <param name="data">A list of key-value pairs that contains properties to change.</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	/// <returns>The record updated.</returns>
	Task<T> Patch<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes custom SurrealQL queries.
	/// </summary>
	/// <param name="query">The SurrealQL query.</param>
	/// <param name="parameters">A list of parameters to be used inside the SurrealQL query.</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	/// <returns>The list of results from the SurrealQL queries.</returns>
	Task<SurrealDbResponse> Query(string query, IReadOnlyDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
	
    /// <summary>
    /// Selects all records in a table.
    /// </summary>
    /// <typeparam name="T">The type of record to extract</typeparam>
    /// <param name="table">The name of the database table</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of extracted records</returns>
    Task<List<T>> Select<T>(string table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a single record.
    /// </summary>
    /// <typeparam name="T">The type of the record</typeparam>
    /// <param name="table">The name of the database table</param>
    /// <param name="id">The id of the record</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The extracted record</returns>
    Task<T?> Select<T>(string table, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a single record.
    /// </summary>
    /// <typeparam name="T">The type of the record</typeparam>
    /// <param name="thing">The record id.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The extracted record</returns>
    Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a value as a parameter for this connection.
    /// </summary>
    /// <param name="key">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task Set(string key, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign in as a root user.
    /// </summary>
    /// <param name="root">Credentials to sign in as a root user</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task SignIn(RootAuth root, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sign in as a namespace user.
	/// </summary>
	/// <param name="nsAuth">Credentials to sign in as a namespace user</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	Task SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sign in as a database user.
	/// </summary>
	/// <param name="dbAuth">Credentials to sign in as a database user</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	Task SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sign in as a scoped user.
	/// </summary>
	/// <typeparam name="T">Type of the params used in the SIGNIN scope function</typeparam>
	/// <param name="scopeAuth">Credentials to sign in as a scoped user</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	/// <returns>The JSON Web Token that can be used to authenticate the user</returns>
	Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken = default) where T : ScopeAuth;

	/// <summary>
	/// Sign up a new scoped user.
	/// </summary>
	/// <typeparam name="T">Type of the params used in the SIGNUP scope function</typeparam>
	/// <param name="scopeAuth">Credentials to sign up as a scoped user</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	/// <returns>The JSON Web Token that can be used to authenticate the user</returns>
	Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken = default) where T : ScopeAuth;

	/// <summary>
	/// Removes a parameter from this connection.
	/// </summary>
	/// <param name="key">The name of the parameter.</param>
	void Unset(string key);

	/// <summary>
	/// Updates or creates the specified record in the database.
	/// </summary>
	/// <typeparam name="T">The type of the record to create.</typeparam>
	/// <param name="data">The record to create or update.</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	/// <returns>The record created or updated.</returns>
	Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default) where T : Record;

	/// <summary>
	/// Switch to a specific namespace and database.
	/// </summary>
	/// <param name="ns">Name of the namespace</param>
	/// <param name="db">Name of the database</param>
	/// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
	Task Use(string ns, string db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the version of the SurrealDB instance.
    /// </summary>
    /// <returns>The version of the SurrealDB instance</returns>
    Task<string> Version();
}
