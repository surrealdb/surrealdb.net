namespace SurrealDB.Client.FSharp

open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
module Json =
    /// <summary>
    /// Creates a new <see cref="JsonSerializerOptions"/> instance from the given <see cref="JsonFSharpOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="JsonFSharpOptions"/> to use.</param>
    /// <returns>A new <see cref="JsonSerializerOptions"/> instance.</returns>
    /// <remarks>
    /// The <see cref="JsonFSharpOptions"/> is configured to skip null fields on option types.
    /// </remarks>
    let configOptionsFrom (options: JsonFSharpOptions) =
        let options =
            options
                .WithSkippableOptionFields(true)
                .ToJsonSerializerOptions()

        options.Converters.Insert(0, SurrealIdConverter())

        options

    /// <summary>
    /// Creates a new <see cref="JsonSerializerOptions"/> instance from the default
    /// <see cref="JsonFSharpOptions"/>.
    /// </summary>
    let configOptions () =
        JsonFSharpOptions.Default()
        |> configOptionsFrom

    /// <summary>
    /// The default <see cref="JsonSerializerOptions"/> instance.
    /// </summary>
    /// <remarks>
    /// Use this instance as much as possible to avoid creating new instances, which is expensive.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/configure-options?pivots=dotnet-6-0"/>
    /// </remarks>
    let defaultOptions = configOptions()
