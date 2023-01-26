namespace SurrealDB.Client.FSharp

open System

[<Struct>]
type ServerInfo =
    { version: string voption
      name: string voption }

[<Struct>]
type ResponseInfo =
    { timeString: string voption
      time: TimeSpan voption Lazy
      date: DateTimeOffset voption Lazy
      dateString: string voption }

type SurrealError =
    | ForbiddenError of ErrorDetails
    | BadRequestError of ErrorDetails
    | ConnectionError of exn

and [<Struct>] ErrorDetails =
    { details: string voption
      code: int
      description: string voption
      information: string voption
      response: ResponseInfo
      server: ServerInfo }
