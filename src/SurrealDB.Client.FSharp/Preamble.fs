[<AutoOpen>]
module internal SurrealDB.Client.FSharp.Preamble

open System
open System.Collections.Generic
open System.Globalization
open System.Text
open System.Text.Json
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module ValueOption =
    let ofOption = function
        | Some x -> ValueSome x
        | None -> ValueNone

[<RequireQualifiedAccess>]
module Seq =
    let inline getEnumerator (source: seq<'T>) = source.GetEnumerator()
    let inline moveNext (enumerator: IEnumerator<'T>) = enumerator.MoveNext()
    let inline getCurrent (enumerator: IEnumerator<'T>) = enumerator.Current

    let tryHeadValue source =
        use enumerator = getEnumerator source

        if moveNext enumerator then
            ValueSome(getCurrent enumerator)
        else
            ValueNone

[<RequireQualifiedAccess>]
module String =
    let inline isEmpty (s: string) = String.IsNullOrEmpty s
    let inline isWhiteSpace (s: string) = String.IsNullOrWhiteSpace s

    let inline internal toBase64 (s: string) =
        Convert.ToBase64String(Encoding.UTF8.GetBytes(s))

    let orEmpty s = if isNull s then "" else s

[<RequireQualifiedAccess>]
module Double =
    let tryParse (s: string) =
        match Double.TryParse(s, NumberStyles.Currency, CultureInfo.InvariantCulture) with
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
    let regex =
        Regex(@"^(?<amount>\d+(\.\d+)?)(?<unit>s|ms|µs|ns)$", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let internal unitsToSeconds unit' =
        match unit' with
        | "s" -> ValueSome 1.0
        | "ms" -> ValueSome 1e-3
        | "µs" -> ValueSome 1e-6
        | "ns" -> ValueSome 1e-9
        | _ -> ValueNone

    let internal fromMatch (match': Match) =
        let amount =
            Double.tryParse (match'.Groups.["amount"].Value)

        let seconds =
            unitsToSeconds (match'.Groups.["unit"].Value)

        match amount, seconds with
        | ValueSome amount, ValueSome seconds -> ValueSome(TimeSpan.FromSeconds(amount * seconds))
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
// else
//     regex.Match(s)
//     |> fun match' -> if match'.Success then
//     if match'.Success then
//         match Double.TryParse(match'.Groups.["amount"].Value) with
//         | true, amount -> ValueSome (TimeSpan.FromTicks((Int64)(amount * (Double)TimeSpan.TicksPerSecond)))
//         let amount = Decimal.Parse(match'.Groups.["amount"].Value, CultureInfo.InvariantCulture)
//         let unit = match'.Groups.["unit"].Value
//         let ticks =
//             match unit with
//             | "s" -> TimeSpan.TicksPerSecond
//             | "ms" -> TimeSpan.TicksPerMillisecond
//             | "µs" -> TimeSpan.TicksPerMillisecond / 1000L
//             | "ns" -> 1L
//             | _ -> failwithf "Invalid time unit: %s" unit

//         ValueSome (TimeSpan((Int64)(amount * (Decimal)ticks)))
//     else
//         ValueNone

[<RequireQualifiedAccess>]
module Json =
    let defaultOptions =
        let o = JsonSerializerOptions()
        // o.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        o

    let deserialize<'a> (json: string) =
        JsonSerializer.Deserialize<'a>(json, defaultOptions)

    let serialize<'a> (data: 'a) =
        JsonSerializer.Serialize<'a>(data, defaultOptions)
