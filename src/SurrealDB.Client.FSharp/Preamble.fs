[<AutoOpen>]
module internal SurrealDB.Client.FSharp.Preamble

open System
open System.Collections.Generic
open System.Globalization
open System.Text
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module String =
    let isWhiteSpace (s: string) = String.IsNullOrWhiteSpace s

    let internal toBase64 (s: string) =
        Convert.ToBase64String(Encoding.UTF8.GetBytes(s))

[<RequireQualifiedAccess>]
module Double =
    let tryParse (s: string) =
        match Double.TryParse(s, NumberStyles.Currency, CultureInfo.InvariantCulture) with
        | true, date -> ValueSome date
        | false, _ -> ValueNone

[<RequireQualifiedAccess>]
module DateTime =
    let tryParse (s: string) =
        match DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None) with
        | true, date -> ValueSome date
        | false, _ -> ValueNone

[<RequireQualifiedAccess>]
module DateTimeOffset =
    let tryParse (s: string) =
        match DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None) with
        | true, date -> ValueSome date
        | false, _ -> ValueNone

[<RequireQualifiedAccess>]
module TimeSpan =
    let internal regex =
        Regex(@"^(?<amount>\d+(\.\d+)?)(?<unit>s|ms|µs)$", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let internal unitsToSeconds unit' =
        match unit' with
        | "s" -> 1.0
        | "ms" -> 1e-3
        | "µs" -> 1e-6
        | _ -> failwithf "Unknown time unit: %s" unit'

    let internal fromMatch (match': Match) =
        let amount =
            Double.tryParse (match'.Groups.["amount"].Value)

        let seconds =
            unitsToSeconds (match'.Groups.["unit"].Value)

        match amount with
        | ValueSome amount ->
            try
                ValueSome(TimeSpan.FromSeconds(amount * seconds))
            with
            | :? OverflowException -> ValueNone
        | _ -> ValueNone

    let tryParse s =
        if String.isWhiteSpace s then
            ValueNone
        else
            ValueSome s
        |> ValueOption.map (fun s -> regex.Match(s))
        |> ValueOption.bind (fun match' ->
            if match'.Success then
                ValueSome match'
            else
                ValueNone)
        |> ValueOption.bind fromMatch

[<RequireQualifiedAccess>]
module Seq =
    let getEnumerator (source: seq<'T>) = source.GetEnumerator()
    let moveNext (enumerator: IEnumerator<'T>) = enumerator.MoveNext()
    let getCurrent (enumerator: IEnumerator<'T>) = enumerator.Current

    let tryHeadValue source =
        use enumerator = getEnumerator source

        if moveNext enumerator then
            ValueSome(getCurrent enumerator)
        else
            ValueNone
