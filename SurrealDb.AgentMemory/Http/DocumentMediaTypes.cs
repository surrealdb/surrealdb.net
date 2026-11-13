namespace SurrealDb.AgentMemory;

/// <summary>
/// The media type of a document, from its file name.
/// </summary>
/// <remarks>
/// Only the extensions the service ingests are listed, with the types it names for them. Anything else
/// has no entry: the file is then sent without a type and the service works it out from the bytes.
/// </remarks>
internal static class DocumentMediaTypes
{
    private static readonly Dictionary<string, string> ByExtension = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["txt"] = "text/plain",
        ["md"] = "text/markdown",
        ["markdown"] = "text/markdown",
        ["csv"] = "text/csv",
        ["html"] = "text/html",
        ["htm"] = "text/html",
        ["json"] = "application/json",
        ["pdf"] = "application/pdf",
        ["png"] = "image/png",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["webp"] = "image/webp",
        ["gif"] = "image/gif",
        ["wav"] = "audio/wav",
        ["mp3"] = "audio/mpeg",
        ["ogg"] = "audio/ogg",
        ["flac"] = "audio/flac",
        ["aac"] = "audio/aac",
        ["mp4"] = "video/mp4",
        ["webm"] = "video/webm",
        ["mov"] = "video/quicktime",
    };

    /// <summary>
    /// The media type for a file name's extension, or <see langword="null"/> when the name has none or
    /// the extension is not one the service ingests.
    /// </summary>
    public static string? ForFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var dot = fileName.LastIndexOf('.');
        if (dot < 0 || dot == fileName.Length - 1)
        {
            return null;
        }

        return ByExtension.GetValueOrDefault(fileName[(dot + 1)..]);
    }
}
