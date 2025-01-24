using System.Collections.Concurrent;
using System.Text;

namespace SurrealDb.Net.Tests.Fixtures;

public static class SurrealDbClientExtensions
{
    private static readonly ConcurrentDictionary<SurrealSchemaFile, string> _schemaDefinitions =
        new();

    public static async Task ApplySchemaAsync(
        this SurrealDbClient client,
        SurrealSchemaFile schemaFile,
        CancellationToken cancellationToken = default
    )
    {
        string query = await RetrieveSchemaDefinitionAsync(schemaFile, cancellationToken);
        (await client.RawQuery(query, cancellationToken: cancellationToken)).EnsureAllOks();
    }

    private static async ValueTask<string> RetrieveSchemaDefinitionAsync(
        SurrealSchemaFile schemaFile,
        CancellationToken cancellationToken
    )
    {
        if (_schemaDefinitions.TryGetValue(schemaFile, out var schemaDefinition))
        {
            return schemaDefinition;
        }

        string schemaFileName = schemaFile switch
        {
            SurrealSchemaFile.Datetime => "datetime",
            SurrealSchemaFile.Decimal => "decimal",
            SurrealSchemaFile.Duration => "duration",
            SurrealSchemaFile.Geometry => "geometry",
            SurrealSchemaFile.Number => "number",
            SurrealSchemaFile.Post => "post",
            SurrealSchemaFile.RecordId => "recordId",
            SurrealSchemaFile.String => "string",
            SurrealSchemaFile.User => "user",
            SurrealSchemaFile.Uuid => "uuid",
            SurrealSchemaFile.Vector => "vector",
            _ => throw new NotImplementedException(),
        };

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            $"Schemas/{schemaFileName}.surql"
        );
        string fileContent = await File.ReadAllTextAsync(
            filePath,
            Encoding.UTF8,
            cancellationToken
        );

        _schemaDefinitions.TryAdd(schemaFile, fileContent);

        return fileContent;
    }
}
