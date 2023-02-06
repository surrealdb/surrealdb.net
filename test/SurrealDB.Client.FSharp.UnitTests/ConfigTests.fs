module SurrealDB.Client.FSharp.ConfigTests

open System
open Xunit
open FsCheck.Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp

[<Fact>]
let ``SurrealCredentials.basicCredentials`` () =
    let testCases =
        [ "root", "root", Ok(Basic("root", "root"))
          "  root  ", "\troot\n", Ok(Basic("root", "root"))
          null, null, Error BasicCredentialsError.InvalidUser
          "", null, Error BasicCredentialsError.InvalidUser
          String('u', 101), null, Error BasicCredentialsError.InvalidUser
          "root", null, Error BasicCredentialsError.InvalidPassword
          "root", "", Error BasicCredentialsError.InvalidPassword
          "root", String('u', 101), Error BasicCredentialsError.InvalidPassword

          ]

    for (user, password, expected) in testCases do
        test <@ SurrealCredentials.basicCredentials user password = expected @>

[<Fact>]
let ``SurrealCredentials.bearerCredentials`` () =
    let sampleJwt =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

    let testCases =
        [ sampleJwt, Ok(Bearer(sampleJwt))
          $"  {sampleJwt}\t\n", Ok(Bearer(sampleJwt))
          null, Error BearerCredentialsError.InvalidJwt
          "", Error BearerCredentialsError.InvalidJwt
          String('u', 0x2001), Error BearerCredentialsError.InvalidJwt ]

    for (jwt, expected) in testCases do
        test <@ SurrealCredentials.bearerCredentials jwt = expected @>

[<Fact>]
let ``SurrealConfig.empty`` () =
    test <@ SurrealConfig.empty.baseUrl = "http://localhost:8000" @>
    test <@ SurrealConfig.empty.credentials = ValueNone @>
    test <@ SurrealConfig.empty.ns = "" @>
    test <@ SurrealConfig.empty.db = "" @>

[<Fact>]
let ``SurrealConfig.withBaseUrl`` () =
    let testCases =
        [ "http://localhost:8000", Ok { SurrealConfig.empty with baseUrl = "http://localhost:8000" }
          "https://localhost:8000", Ok { SurrealConfig.empty with baseUrl = "https://localhost:8000" }
          "https://cloud.surrealdb.com", Ok { SurrealConfig.empty with baseUrl = "https://cloud.surrealdb.com" }
          null, Error ConfigError.InvalidBaseUrl
          "", Error ConfigError.InvalidBaseUrl
          $"http://{String('s', 256)}", Error ConfigError.InvalidBaseUrl
          "invalid url", Error ConfigError.InvalidBaseUrl
          "relative/url", Error ConfigError.InvalidBaseUrl ]

    for (baseUrl, expected) in testCases do
        test <@ SurrealConfig.withBaseUrl baseUrl SurrealConfig.empty = expected @>

[<Property>]
let ``SurrealConfig.withCredentials`` (credentials: SurrealCredentials) =
    let expected =
        { SurrealConfig.empty with credentials = ValueSome credentials }

    test <@ SurrealConfig.withCredentials credentials SurrealConfig.empty = expected @>

[<Fact>]
let ``SurrealConfig.withBasicCredentials`` () =
    let testCases =
        [ "root", "root", Ok({ SurrealConfig.empty with credentials = ValueSome(Basic("root", "root")) })
          null, null, Error(ConfigError.InvalidBasicCredentials BasicCredentialsError.InvalidUser)
          "root", null, Error(ConfigError.InvalidBasicCredentials BasicCredentialsError.InvalidPassword)]

    for (user, password, expected) in testCases do
        test <@ SurrealConfig.withBasicCredentials user password SurrealConfig.empty = expected @>

[<Fact>]
let ``SurrealConfig.withBearerCredentials`` () =
    let sampleJwt =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

    let testCases =
        [ sampleJwt, Ok({ SurrealConfig.empty with credentials = ValueSome(Bearer(sampleJwt)) })
          null, Error(ConfigError.InvalidBearerCredentials BearerCredentialsError.InvalidJwt)]

    for (jwt, expected) in testCases do
        test <@ SurrealConfig.withBearerCredentials jwt SurrealConfig.empty = expected @>

[<Fact>]
let ``SurrealConfig.withNamespace`` () =
    let testCases =
        [ "testns", Ok { SurrealConfig.empty with ns = "testns" }
          null, Error ConfigError.InvalidNamespace
          "", Error ConfigError.InvalidNamespace
          String('s', 257), Error ConfigError.InvalidNamespace ]

    for (ns, expected) in testCases do
        test <@ SurrealConfig.withNamespace ns SurrealConfig.empty = expected @>

[<Fact>]
let ``SurrealConfig.withDatabase`` () =
    let testCases =
        [ "testdb", Ok { SurrealConfig.empty with db = "testdb" }
          null, Error ConfigError.InvalidDatabase
          "", Error ConfigError.InvalidDatabase
          String('s', 257), Error ConfigError.InvalidDatabase ]

    for (db, expected) in testCases do
        test <@ SurrealConfig.withDatabase db SurrealConfig.empty = expected @>
