namespace SurrealDB.Client.FSharp

open System
open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp

[<Trait(Category, UnitTest)>]
[<Trait(Area, COMMON)>]
module ConfigTests =

    let expectErrors expectedErrors (configResult: Result<SurrealConfig, SurrealConfigError list>) =
        match configResult with
        | Ok config -> Assert.Fail(sprintf "Expected errors %A, got config: %A" expectedErrors config)
        | Error errors ->
            for error in expectedErrors do
                Assert.Contains(error, errors)

    let expectValid (baseUrl, ns, db, credentials) (configResult: Result<SurrealConfig, SurrealConfigError list>) =
        match configResult with
        | Ok config ->
            test <@ baseUrl = config.BaseUrl @>
            test <@ ns = config.Namespace @>
            test <@ db = config.Database @>
            test <@ credentials = config.Credentials @>
        | Error errors -> Assert.Fail(sprintf "Expected config %A, got errors: %A" (baseUrl, ns, db, credentials) errors)

    [<Fact>]
    let ``Building empty configuration`` () =
        SurrealConfig.Builder().Build()
        |> expectErrors [ InvalidBaseUrl SurrealConfig.EMPTY
                          InvalidNamespace SurrealConfig.EMPTY
                          InvalidDatabase SurrealConfig.EMPTY ]

    let invalidBaseUrlData () =
        seq {
            [| null; SurrealConfig.EMPTY |]
            [| ""; SurrealConfig.EMPTY |]
            [| " \r\n\t\f"; SurrealConfig.EMPTY |]

            [| "invalid url"
               SurrealConfig.FORMAT |]

            [| "relative/url"
               SurrealConfig.FORMAT |]

            [| $"http://example.com/{String('b', 256)}"
               SurrealConfig.TOO_LONG |]
        }

    [<Theory>]
    [<MemberData(nameof (invalidBaseUrlData))>]
    let ``Building configuration with invalid base url`` (value: string) (error: string) =
        SurrealConfig.Builder().WithBaseUrl(value).Build()
        |> expectErrors [ InvalidBaseUrl error ]

    let invalidNsOrDbData () =
        seq {
            [| null; SurrealConfig.EMPTY |]
            [| ""; SurrealConfig.EMPTY |]
            [| " \r\n\t\f"; SurrealConfig.EMPTY |]

            [| String('n', 257)
               SurrealConfig.TOO_LONG |]
        }

    [<Theory>]
    [<MemberData(nameof (invalidNsOrDbData))>]
    let ``Building configuration with invalid namespace`` (value: string) (error: string) =
        SurrealConfig
            .Builder()
            .WithNamespace(value)
            .Build()
        |> expectErrors [ InvalidNamespace error ]

    [<Theory>]
    [<MemberData(nameof (invalidNsOrDbData))>]
    let ``Building configuration with invalid database`` (value: string) (error: string) =
        SurrealConfig
            .Builder()
            .WithDatabase(value)
            .Build()
        |> expectErrors [ InvalidDatabase error ]

    let invalidJWTData () =
        seq {
            [| null; SurrealConfig.EMPTY |]
            [| ""; SurrealConfig.EMPTY |]
            [| " \r\n\t\f"; SurrealConfig.EMPTY |]

            [| String('j', 8197)
               SurrealConfig.TOO_LONG |]
        }

    [<Theory>]
    [<MemberData(nameof (invalidJWTData))>]
    let ``Building configuration with invalid JWT bearer token`` (value: string) (error: string) =
        SurrealConfig
            .Builder()
            .WithBearerCredentials(value)
            .Build()
        |> expectErrors [ InvalidJWT error ]

    let invalidBasicData () =
        seq {
            [| null :> obj
               null
               [ InvalidUser SurrealConfig.EMPTY
                 InvalidPassword SurrealConfig.EMPTY ] |]

            [| " \r\n\t\f" :> obj
               null
               [ InvalidUser SurrealConfig.EMPTY
                 InvalidPassword SurrealConfig.EMPTY ] |]

            [| String('u', 101) :> obj
               null
               [ InvalidUser SurrealConfig.TOO_LONG
                 InvalidPassword SurrealConfig.EMPTY ] |]

            [| "root" :> obj
               null
               [ InvalidPassword SurrealConfig.EMPTY ] |]

            [| null :> obj
               " \r\n\t\f"
               [ InvalidUser SurrealConfig.EMPTY
                 InvalidPassword SurrealConfig.EMPTY ] |]

            [| null :> obj
               String('p', 101)
               [ InvalidUser SurrealConfig.EMPTY
                 InvalidPassword SurrealConfig.TOO_LONG ] |]

            [| null :> obj
               "root"
               [ InvalidUser SurrealConfig.EMPTY ] |]
        }

    [<Theory>]
    [<MemberData(nameof (invalidBasicData))>]
    let ``Building configuration with invalid basic credentials``
        (user: string)
        (password: string)
        (errors: SurrealConfigError list)
        =
        SurrealConfig
            .Builder()
            .WithBasicCredentials(user, password)
            .Build()
        |> expectErrors errors

    [<Fact>]
    let ``Building valid configuration without credentials`` () =
        SurrealConfig
            .Builder()
            .WithBaseUrl("http://localhost:8010")
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .Build()
        |> expectValid ("http://localhost:8010", "testns", "testdb", ValueNone)

    [<Fact>]
    let ``Building valid configuration with default base url`` () =
        SurrealConfig
            .Builder()
            .WithDefaultBaseUrl()
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .Build()
        |> expectValid ("http://localhost:8000", "testns", "testdb", ValueNone)

    [<Fact>]
    let ``Building valid configuration with basic credentials`` () =
        SurrealConfig
            .Builder()
            .WithBaseUrl("http://localhost:8010")
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .WithBasicCredentials("root", "root")
            .Build()
        |> expectValid ("http://localhost:8010", "testns", "testdb", ValueSome(SurrealCredentials.Basic("root", "root")))

    [<Fact>]
    let ``Building valid configuration with bearer credentials`` () =
        let johnDoeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
        SurrealConfig
            .Builder()
            .WithBaseUrl("http://localhost:8010")
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .WithBearerCredentials(johnDoeToken)
            .Build()
        |> expectValid ("http://localhost:8010", "testns", "testdb", ValueSome(SurrealCredentials.Bearer(johnDoeToken)))
