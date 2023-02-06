namespace SurrealDB.Client.FSharp

type SurrealCredentials =
    | Basic of user: string * password: string
    | Bearer of jwt: string

/// <summary>
/// Represents the possible errors when creating a Basic authentication credential.
/// </summary>
[<RequireQualifiedAccess>]
type BasicCredentialsError =
    /// Issued when the user is Invalid.
    | InvalidUser
    /// Issued when the password is Invalid.
    | InvalidPassword


/// <summary>
/// Represents the possible errors when creating a Bearer authentication credential.
/// </summary>
[<RequireQualifiedAccess>]
type BearerCredentialsError =
    /// Issued when the JWT is invalid.
    | InvalidJwt

module SurrealCredentials =
    [<Literal>]
    let internal MAX_BASIC_DATA_LENGTH = 100

    /// <summary>
    /// Creates a Basic authentication credential.
    /// Validates that the user and password are not empty or too long.
    /// </summary>
    /// <param name="user">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A result containing the credential or an error message.</returns>
    let basicCredentials user password =
        let user = String.trimIfNotNull user
        let password = String.trimIfNotNull password
        if String.isWhiteSpace user
           || String.length user > MAX_BASIC_DATA_LENGTH then
            Error BasicCredentialsError.InvalidUser
        elif String.isWhiteSpace password
             || String.length password > MAX_BASIC_DATA_LENGTH then
            Error BasicCredentialsError.InvalidPassword
        else
            Ok <| Basic(user, password)

    [<Literal>]
    let internal MAX_JWT_LENGTH = 8192

    /// <summary>
    /// Creates a Bearer authentication credential.
    /// Validates that the JWT is not empty or too long.
    /// </summary>
    /// <param name="jwt">The JWT.</param>
    /// <returns>A result containing the credential or an error message.</returns>
    let bearerCredentials jwt =
        let jwt = String.trimIfNotNull jwt
        if String.isWhiteSpace jwt
           || String.length jwt > MAX_JWT_LENGTH then
            Error BearerCredentialsError.InvalidJwt
        else
            Ok <| Bearer jwt

[<Struct>]
type SurrealConfig =
    { baseUrl: string
      credentials: SurrealCredentials voption
      ns: string
      db: string }

/// <summary>
/// Represents the possible errors when creating a SurrealDB configuration.
/// </summary>
[<RequireQualifiedAccess>]
type ConfigError =
    /// Issued when the base url is invalid.
    | InvalidBaseUrl
    /// Issued when the basic credentials are invalid.
    | InvalidBasicCredentials of BasicCredentialsError
    /// Issued when the bearer credentials are invalid.
    | InvalidBearerCredentials of BearerCredentialsError
    /// Issued when the namespace is invalid.
    | InvalidNamespace
    /// Issued when the database is invalid.
    | InvalidDatabase
    /// Missing database.
    | MissingDatabase

module SurrealConfig =
    [<AutoOpen>]
    module Constants =
        [<Literal>]
        let internal DEFAULT_BASEURL = "http://localhost:8000"

        [<Literal>]
        let internal MAX_BASEURL_LENGTH = 256

        [<Literal>]
        let internal MAX_NAMESPACE_LENGTH = 256

        [<Literal>]
        let internal MAX_DATABASE_LENGTH = 256

    /// <summary>
    /// Holds a SurrealDB default configuration.
    /// Use pipe operators to modify the configuration, using the methods in this module,
    /// in combination with <code>Result.map</code> and <code>Result.bind</code>.
    /// </summary>
    /// <example>
    /// <code>
    /// let config =
    ///    SurrealConfig.empty
    ///    |> SurrealConfig.withBaseUrl "http://localhost:8080"
    ///    |> Result.bind (SurrealConfig.withBasicCredentials "root" "root")
    ///    |> Result.bind (SurrealConfig.withNamespace "testns")
    ///    |> Result.bind (SurrealConfig.withDatabase "testdb")
    /// </code>
    /// </example>
    let empty =
        { baseUrl = DEFAULT_BASEURL
          credentials = ValueNone
          ns = ""
          db = "" }

    let withBaseUrl baseUrl config =
        if String.isWhiteSpace baseUrl
           || String.length baseUrl > MAX_BASEURL_LENGTH
           || not(System.Uri.IsWellFormedUriString(baseUrl, System.UriKind.Absolute)) then
            Error ConfigError.InvalidBaseUrl
        else
            Ok { config with baseUrl = baseUrl }

    let withCredentials credentials config =
        { config with credentials = ValueSome credentials }

    let withBasicCredentials user password config =
        SurrealCredentials.basicCredentials user password
        |> Result.map (fun credentials -> withCredentials credentials config)
        |> Result.mapError ConfigError.InvalidBasicCredentials

    let withBearerCredentials jwt config =
        SurrealCredentials.bearerCredentials jwt
        |> Result.map (fun credentials -> withCredentials credentials config)
        |> Result.mapError ConfigError.InvalidBearerCredentials

    let withNamespace ns config =
        if String.isWhiteSpace ns
           || String.length ns > MAX_NAMESPACE_LENGTH then
            Error ConfigError.InvalidNamespace
        else
            Ok { config with ns = ns }

    let withDatabase db config =
        if String.isWhiteSpace db
           || String.length db > MAX_DATABASE_LENGTH then
            Error ConfigError.InvalidDatabase
        else
            Ok { config with db = db }
