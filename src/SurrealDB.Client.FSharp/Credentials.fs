namespace SurrealDB.Client.FSharp

type SurrealCredentials =
    | Basic of user: string * password: string
    | Bearer of jwt: string

[<AutoOpen>]
module Credentials =
    open System
    open System.Text

    /// <summary>
    /// Represents the possible errors when creating a Basic authentication credential.
    /// </summary>
    [<RequireQualifiedAccess>]
    type BasicCredentialsError =
        /// Issued when the user is empty.
        | EmptyUser
        /// Issued when the user is too long.
        | UserTooLong
        /// Issued when the password is empty.
        | EmptyPassword
        /// Issued when the password is too long.
        | PasswordTooLong

    [<Literal>]
    let internal MAX_BASIC_DATA_LENGTH = 100

    /// <summary>
    /// Creates a Basic authentication credential.
    /// Validates that the user and password are not empty or too long.
    /// <param name="user">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A result containing the credential or an error message.</returns>
    /// </summary>
    let basicCredentials user password =
        if String.isWhiteSpace user then
            Error BasicCredentialsError.EmptyUser
        elif String.length user > MAX_BASIC_DATA_LENGTH then
            Error BasicCredentialsError.UserTooLong
        elif String.isWhiteSpace password then
            Error BasicCredentialsError.EmptyPassword
        elif String.length password > MAX_BASIC_DATA_LENGTH then
            Error BasicCredentialsError.PasswordTooLong
        else
            Ok <| Basic(user, password)

    /// <summary>
    /// Represents the possible errors when creating a Bearer authentication credential.
    /// </summary>
    [<RequireQualifiedAccess>]
    type BearerCredentialsError =
        /// Issued when the JWT is empty.
        | EmptyJwt
        /// Issued when the JWT is too long.
        | JwtTooLong

    [<Literal>]
    let internal MAX_BEARER_DATA_LENGTH = 8192

    /// <summary>
    /// Creates a Bearer authentication credential.
    /// Validates that the JWT is not empty or too long.
    /// <param name="jwt">The JWT.</param>
    /// <returns>A result containing the credential or an error message.</returns>
    /// </summary>
    let bearerCredentials jwt =
        if String.isWhiteSpace jwt then
            Error BearerCredentialsError.EmptyJwt
        elif String.length jwt > MAX_BEARER_DATA_LENGTH then
            Error BearerCredentialsError.JwtTooLong
        else
            Ok <| Bearer jwt

    /// <summary>
    /// Converts a credential to a list of HTTP headers.
    /// </summary>
    let toHttpHeaders =
        function
        | Basic (user, password) ->
            let auth =
                String.toBase64 <| sprintf "%s:%s" user password

            [ "Authorization", sprintf "Basic %s" auth ]
        | Bearer jwt -> [ "Authorization", sprintf "Bearer %s" jwt ]
