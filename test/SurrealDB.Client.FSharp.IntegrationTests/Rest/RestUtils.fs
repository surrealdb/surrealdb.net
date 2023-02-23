[<AutoOpen>]
module SurrealDB.Client.FSharp.Rest.RestUtils

open System.Collections.Generic
open System.Text.Json.Nodes
open System
open System.Globalization
open System.Net.Http

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

type JsonDiff =
    { path: JsonPathStep list
      diff: JsonMismatch }

and JsonPathStep =
    | JsonIndex of int
    | JsonKey of string

and JsonMismatch =
    | StringValueMismatch of leftValue: string * rightValue: string
    | Int32ValueMismatch of leftValue: int32 * rightValue: int32
    | NumberValueMismatch of leftValue: float * rightValue: float
    | BooleanValueMismatch of leftValue: bool * rightValue: bool
    | ArrayLengthMismatch of leftLength: int * rightLength: int * leftArray: JsonArray * rightArray: JsonArray
    | TypeMismatch of leftType: string * rightType: string * leftNode: JsonNode * rightNode: JsonNode
    | MissingLeftKey of leftKey: string * rightNode: JsonNode
    | MissingRightKey of rightKey: string * leftNode: JsonNode
    | UnknownJsonNodes of leftUnknown: bool * rightUnknown: bool * leftNode: JsonNode * rightNode: JsonNode

let jsonDiff expectedFn (node1: JsonNode) (node2: JsonNode) =
    let diffs = ResizeArray()

    let rec loop path (node1: JsonNode) (node2: JsonNode) =

        let addError diff =
            let path = path |> List.rev
            let error = { path = path; diff = diff }

            if not (expectedFn error) then
                diffs.Add error

        match node1, node2 with
        | IsJsonString value1, IsJsonString value2 ->
            if value1 <> value2 then
                addError <| StringValueMismatch(value1, value2)
        | IsJsonInt32 value1, IsJsonInt32 value2 ->
            if value1 <> value2 then
                addError <| Int32ValueMismatch(value1, value2)
        | IsJsonNumber value1, IsJsonNumber value2 ->
            if value1 <> value2 then
                addError <| NumberValueMismatch(value1, value2)
        | IsJsonBoolean value1, IsJsonBoolean value2 ->
            if value1 <> value2 then
                addError <| BooleanValueMismatch(value1, value2)
        | IsJsonNull (), IsJsonNull () -> ()

        | IsJsonArray array1, IsJsonArray array2 ->
            if array1.Count <> array2.Count then
                addError
                <| ArrayLengthMismatch(array1.Count, array2.Count, array1, array2)
            else
                for i in 0 .. array1.Count - 1 do
                    loop (JsonIndex i :: path) array1.[i] array2.[i]

        | IsJsonObject obj1, IsJsonObject obj2 ->
            let dict1: IDictionary<string, JsonNode> = obj1
            let dict2: IDictionary<string, JsonNode> = obj2

            let allKeys =
                dict1.Keys
                |> Seq.append dict2.Keys
                |> Seq.distinct

            for key in allKeys do
                match dict1.TryGetValue key, dict2.TryGetValue key with
                | (true, value1), (true, value2) -> loop (JsonKey key :: path) value1 value2
                | (true, value1), (false, _) -> addError <| MissingRightKey(key, value1)
                | (false, _), (true, value2) -> addError <| MissingLeftKey(key, value2)
                | (false, _), (false, _) -> failwith "This should never happen"

        | JsonNodeType type1, JsonNodeType type2 when type1 <> type2 ->
            addError
            <| TypeMismatch(type1, type2, node1, node2)
        | JsonNodeType _, node2 ->
            addError
            <| UnknownJsonNodes(false, true, node1, node2)
        | node1, JsonNodeType _ ->
            addError
            <| UnknownJsonNodes(true, false, node1, node2)
        | node1, node2 ->
            addError
            <| UnknownJsonNodes(true, true, node1, node2)

    loop [] node1 node2

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
