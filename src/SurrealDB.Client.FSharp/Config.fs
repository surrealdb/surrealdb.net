namespace SurrealDB.Client.FSharp

open System

/// <summary>
/// Represents the possible authentication credentials.
/// </summary>
[<RequireQualifiedAccess>]
type SurrealCredentials =
    /// <summary>
    /// Basic authentication, with user and password.
    /// </summary>
    /// <param name="user">The user name.</param>
    /// <param name="password">The password.</param>
    /// <remarks>
    /// The user and password are sent in plain text, or encoded in base64, which should be considered insecure.
    /// </remarks>
    | Basic of user: string * password: string
    /// <summary>
    /// Bearer authentication, with a JWT.
    /// </summary>
    /// <param name="jwt">The JWT.</param>
    | Bearer of jwt: string


/// <summary>
/// Represents the possible errors when building a SurrealConfig.
/// </summary>
type SurrealConfigError =
    /// Issued when the user is Invalid.
    | InvalidUser of string
    /// Issued when the password is Invalid.
    | InvalidPassword of string
    /// Issued when the bearer token is Invalid.
    | InvalidJWT of string
    /// Issued when the namespace is Invalid.
    | InvalidNamespace of string
    /// Issued when the database is Invalid.
    | InvalidDatabase of string
    /// Issued when the base url is Invalid.
    | InvalidBaseUrl of string

[<RequireQualifiedAccess>]
module SurrealConfig =
    [<Literal>]
    let MAX_BASIC_DATA_LENGTH = 100

    [<Literal>]
    let MAX_JWT_LENGTH = 8192

    [<Literal>]
    let DEFAULT_BASEURL = "http://localhost:8000"

    [<Literal>]
    let MAX_BASEURL_LENGTH = 256

    [<Literal>]
    let MAX_NAMESPACE_LENGTH = 256

    [<Literal>]
    let MAX_DATABASE_LENGTH = 256

    [<Literal>]
    let EMPTY = "EMPTY"

    [<Literal>]
    let TOO_LONG = "TOO_LONG"

    [<Literal>]
    let FORMAT = "FORMAT"

/// <summary>
/// Represents a validated configuration for a SurrealDB client.
/// Use the <see cref="SurrealConfig.Builder"/> static method to start building a configuration.
/// Use the <see cref="SurrealConfigBuilder.Build"/> instance method to validate and build the configuration.
/// </summary>
/// <param name="baseUrl">The base url of the SurrealDB server.</param>
/// <param name="ns">The namespace to use.</param>
/// <param name="db">The database to use.</param>
/// <param name="credentials">The authentication credentials to use.</param>
/// <remarks>
/// The configuration is immutable.
/// </remarks>
type SurrealConfig internal (baseUrl, ns, db, credentials) =
    /// <summary>
    /// The base url of the SurrealDB server.
    /// </summary>
    member val BaseUrl = baseUrl

    /// <summary>
    /// The namespace to use.
    /// </summary>
    member val Namespace = ns

    /// <summary>
    /// The database to use.
    /// </summary>
    member val Database = db

    /// <summary>
    /// The authentication credentials to use.
    /// </summary>
    member val Credentials = credentials

    /// <summary>
    /// Creates a new <see cref="SurrealConfigBuilder"/>.
    /// </summary>
    /// <returns>A new SurrealConfigBuilder.</returns>
    static member Builder() = SurrealConfigBuilder()

/// <summary>
/// Allows building and validating a <see cref="SurrealConfig"/>.
/// </summary>
and SurrealConfigBuilder internal () =
    let mutable _ns = null
    let mutable _db = null
    let mutable _baseUrl = null
    let mutable _credentials: SurrealCredentials voption = ValueNone

    /// <summary>
    /// Sets the namespace to use.
    /// </summary>
    member this.WithNamespace ns =
        _ns <- ns
        this

    /// <summary>
    /// Sets the database to use.
    /// </summary>
    member this.WithDatabase db =
        _db <- db
        this

    /// <summary>
    /// Sets the base url of the SurrealDB server.
    /// </summary>
    member this.WithBaseUrl baseUrl =
        _baseUrl <- baseUrl
        this

    /// <summary>
    /// Sets the base url of the SurrealDB server to the default value.
    /// The default value is "http://localhost:8000".
    /// </summary>
    member this.WithDefaultBaseUrl() =
        this.WithBaseUrl(SurrealConfig.DEFAULT_BASEURL)

    member private this.WithCredentials credentials =
        _credentials <- ValueSome credentials
        this

    /// <summary>
    /// Sets the authentication credentials to use as basic authentication.
    /// </summary>
    /// <param name="user">The user name.</param>
    /// <param name="password">The password.</param>
    /// <remarks>
    /// The user and password are sent in plain text, or encoded in base64, which should be considered insecure.
    /// </remarks>
    member this.WithBasicCredentials(user, password) =
        this.WithCredentials(SurrealCredentials.Basic(user, password))

    /// <summary>
    /// Sets the authentication credentials to use as bearer authentication.
    /// </summary>
    /// <param name="jwt">The JWT.</param>
    member this.WithBearerCredentials(jwt) =
        this.WithCredentials(SurrealCredentials.Bearer(jwt))

    /// <summary>
    /// Validates and builds the configuration.
    /// </summary>
    /// <returns>A result containing the configuration, or the errors.</returns>
    member _.Build() =
        let errors =
            [ if String.isWhiteSpace _baseUrl then
                  yield InvalidBaseUrl SurrealConfig.EMPTY
              elif String.length _baseUrl > SurrealConfig.MAX_BASEURL_LENGTH then
                  yield InvalidBaseUrl SurrealConfig.TOO_LONG
              elif not (Uri.IsWellFormedUriString(_baseUrl, UriKind.Absolute)) then
                  yield InvalidBaseUrl SurrealConfig.FORMAT

              if String.isWhiteSpace _ns then
                  yield InvalidNamespace SurrealConfig.EMPTY
              elif String.length _ns > SurrealConfig.MAX_NAMESPACE_LENGTH then
                  yield InvalidNamespace SurrealConfig.TOO_LONG

              if String.isWhiteSpace _db then
                  yield InvalidDatabase SurrealConfig.EMPTY
              elif String.length _db > SurrealConfig.MAX_DATABASE_LENGTH then
                  yield InvalidDatabase SurrealConfig.TOO_LONG

              match _credentials with
              | ValueSome (SurrealCredentials.Basic (user, password)) ->
                  if String.isWhiteSpace user then
                      yield InvalidUser SurrealConfig.EMPTY
                  elif String.length user > SurrealConfig.MAX_BASIC_DATA_LENGTH then
                      yield InvalidUser SurrealConfig.TOO_LONG

                  if String.isWhiteSpace password then
                      yield InvalidPassword SurrealConfig.EMPTY
                  elif String.length password > SurrealConfig.MAX_BASIC_DATA_LENGTH then
                      yield InvalidPassword SurrealConfig.TOO_LONG

              | ValueSome (SurrealCredentials.Bearer (jwt)) ->
                  if String.isWhiteSpace jwt then
                      yield InvalidJWT SurrealConfig.EMPTY
                  elif String.length jwt > SurrealConfig.MAX_JWT_LENGTH then
                      yield InvalidJWT SurrealConfig.TOO_LONG
              | ValueNone -> () ]

        match errors with
        | [] ->
            let config =
                SurrealConfig(baseUrl = _baseUrl, ns = _ns, db = _db, credentials = _credentials)

            Ok config
        | errors -> Error errors
