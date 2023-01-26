[<AutoOpen>]
module internal SurrealDB.Client.FSharp.Preamble

[<RequireQualifiedAccess>]
module Seq =
    open System.Collections.Generic

    let inline getEnumerator (source: seq<'T>) = source.GetEnumerator()
    let inline moveNext (enumerator: IEnumerator<'T>) = enumerator.MoveNext()
    let inline getCurrent (enumerator: IEnumerator<'T>) = enumerator.Current

    let tryHeadValue source =
        use enumerator = getEnumerator source

        if moveNext enumerator then
            ValueSome (getCurrent enumerator)
        else
            ValueNone

[<RequireQualifiedAccess>]
module String =
    open System
    open System.Text

    let inline isEmpty (s: string) = String.IsNullOrEmpty s
    let inline isWhiteSpace (s: string) = String.IsNullOrWhiteSpace s

    let inline internal toBase64 (s: string) =
        Convert.ToBase64String(Encoding.UTF8.GetBytes(s))
