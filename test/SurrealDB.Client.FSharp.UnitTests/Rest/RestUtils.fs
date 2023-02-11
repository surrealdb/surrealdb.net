[<AutoOpen>]
module SurrealDB.Client.FSharp.Rest.RestUtils

open System.Collections.Generic
open System.Text.Json.Nodes
open System
open System.Globalization
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

let private (|JsonNodeType|_|) (node: JsonNode) =
    match node with
    | null -> Some "null"
    | :? JsonArray -> Some "array"
    | :? JsonObject -> Some "object"
    | :? JsonValue as value ->
        match value.TryGetValue<string>() with
        | true, _ -> Some "string"
        | false, _ ->
            match value.TryGetValue<bool>() with
            | true, _ -> Some "boolean"
            | false, _ ->
                match value.TryGetValue<float>() with
                | true, _ -> Some "number"
                | false, _ -> None
    | _ -> None

let private (|IsJsonString|_|) (node: JsonNode) =
    match node with
    | :? JsonValue as value ->
        match value.TryGetValue<string>() with
        | true, value -> Some value
        | false, _ -> None
    | _ -> None

let private (|IsJsonInt32|_|) (node: JsonNode) =
    match node with
    | :? JsonValue as value ->
        match value.TryGetValue<int32>() with
        | true, value -> Some value
        | false, _ -> None
    | _ -> None

let private (|IsJsonNumber|_|) (node: JsonNode) =
    match node with
    | :? JsonValue as value ->
        match value.TryGetValue<float>() with
        | true, value -> Some value
        | false, _ -> None
    | _ -> None

let private (|IsJsonBoolean|_|) (node: JsonNode) =
    match node with
    | :? JsonValue as value ->
        match value.TryGetValue<bool>() with
        | true, value -> Some value
        | false, _ -> None
    | _ -> None

let private (|IsJsonNull|_|) (node: JsonNode) =
    match node with
    | null -> Some()
    | _ -> None

let private (|IsJsonArray|_|) (node: JsonNode) =
    match node with
    | :? JsonArray as array -> Some array
    | _ -> None

let private (|IsJsonObject|_|) (node: JsonNode) =
    match node with
    | :? JsonObject as obj -> Some obj
    | _ -> None

let private appendIndex (index: int) (path: string) =
    if path = "" then
        $"[%d{index}]"
    else
        $"%s{path}[%d{index}]"

let private appendKey (key: string) (path: string) =
    if path = "" then
        $"%s{key}"
    else
        $"%s{path}.%s{key}"

let jsonDiff (node1: JsonNode) (node2: JsonNode) =
    let diffs = ResizeArray()
    let addError error = diffs.Add error

    let rec loop path (node1: JsonNode) (node2: JsonNode) =

        match node1, node2 with
        | IsJsonString value1, IsJsonString value2 ->
            if value1 <> value2 then
                addError $"Value mismatch at path %A{path}: %A{value1} vs %A{value2}"
        | IsJsonInt32 value1, IsJsonInt32 value2 ->
            if value1 <> value2 then
                addError $"Value mismatch at path %A{path}: %A{value1} vs %A{value2}"
        | IsJsonNumber value1, IsJsonNumber value2 ->
            if value1 <> value2 then
                addError $"Value mismatch at path %A{path}: %A{value1} vs %A{value2}"
        | IsJsonBoolean value1, IsJsonBoolean value2 ->
            if value1 <> value2 then
                addError $"Value mismatch at path %A{path}: %A{value1} vs %A{value2}"
        | IsJsonNull (), IsJsonNull () -> ()

        | IsJsonArray array1, IsJsonArray array2 ->
            if array1.Count <> array2.Count then
                addError $"Array length mismatch at path %A{path}: %A{array1.Count} vs %A{array2.Count}"
            else
                for i in 0 .. array1.Count - 1 do
                    loop (appendIndex i path) array1.[i] array2.[i]

        | IsJsonObject obj1, IsJsonObject obj2 ->
            let dict1: IDictionary<string, JsonNode> = obj1
            let dict2: IDictionary<string, JsonNode> = obj2

            let allKeys =
                dict1.Keys
                |> Seq.append dict2.Keys
                |> Seq.distinct
                |> Seq.toList

            for key in allKeys do
                match dict1.TryGetValue key, dict2.TryGetValue key with
                | (true, value1), (true, value2) -> loop (appendKey key path) value1 value2
                | (true, _), (false, _) -> addError $"Missing key at path %A{path}: %A{key}"
                | (false, _), (true, _) -> addError $"Missing key at path %A{path}: %A{key}"
                | (false, _), (false, _) -> failwith "This should never happen"

        | JsonNodeType type1, JsonNodeType type2 when type1 <> type2 ->
            addError $"Type mismatch at path %A{path}: %A{type1} vs %A{type2}"
        | JsonNodeType _, node2 -> addError $"Unknown json node at path %A{path}: %A{node2.GetType().FullName}"
        | node1, JsonNodeType _ -> addError $"Unknown json node at path %A{path}: %A{node1.GetType().FullName}"
        | node1, node2 ->
            addError
                $"Unknown json nodes at path %A{path}: %A{node1.GetType().FullName} vs %A{node2.GetType().FullName}"

    loop "$" node1 node2

    Seq.toList diffs

let resultValue =
    function
    | Ok value -> value
    | Error err -> failwith $"Expected Ok, got Error: %A{err}"

let tryToOption =
    function
    | true, value -> Some value
    | false, _ -> None

let tryGetHeaders name (headers: Headers.HttpRequestHeaders) =
    headers.TryGetValues(name)
    |> tryToOption
    |> Option.map Seq.toList

let PORT = 8000
let USER = "root"
let PASS = "root"
let NS = "testns"
let DB = "testdb"
let DUMMY_VERSION = "dummy-version"
let DUMMY_SERVER = "dummy-server"
let DUMMY_DATE = "Fri, 10 Feb 2023 20:49:37 GMT"

let DUMMY_DATETIME =
    DateTime.Parse(DUMMY_DATE, CultureInfo.InvariantCulture, DateTimeStyles.None)

let DUMMY_DATETIMEOFFSET =
    DateTimeOffset.Parse(DUMMY_DATE, CultureInfo.InvariantCulture, DateTimeStyles.None)
