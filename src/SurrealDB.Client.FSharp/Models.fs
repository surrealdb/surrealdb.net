namespace SurrealDB.Client.FSharp

open System

[<Struct>]
type ServerInfo =
    { version: string
      name: string }
    static member empty = { version = ""; name = "" }

[<Struct>]
type ResponseInfo =
    { timeString: string
      time: TimeSpan voption Lazy
      dateString: string
      date: DateTimeOffset voption Lazy
      status: string
      server: ServerInfo }
    static member empty =
        { timeString = ""
          time = lazy (ValueNone)
          dateString = ""
          date = lazy (ValueNone)
          status = ""
          server = ServerInfo.empty }

type SurrealError =
    | ClientError of ErrorDetails
    | ServerError of ErrorDetails
    | UnknownError of ErrorDetails
    | StatusError of string
    | ConnectionError of exn
    | ParseError of ParseErrorType

and [<Struct>] ErrorDetails =
    { details: string
      code: int
      description: string
      information: string
      response: ResponseInfo }
    static member empty =
        { details = ""
          code = 0
          description = ""
          information = ""
          response = ResponseInfo.empty }

and ParseErrorType =
    | NoContentResponse
    | NoJsonResponse
    | NoResultField
    | NoResultAsCollection
    | NoResultAsObject
