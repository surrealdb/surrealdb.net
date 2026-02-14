using System.Collections.Concurrent;
using System.Text;
using Semver;
using SurrealDb.Net.Extensions;

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
        var version = (await client.Version(cancellationToken)).ToSemver();
        string query = await RetrieveSchemaDefinitionAsync(schemaFile, version, cancellationToken);

        (await client.RawQuery(query, cancellationToken: cancellationToken)).EnsureAllOks();
    }

    private static async ValueTask<string> RetrieveSchemaDefinitionAsync(
        SurrealSchemaFile schemaFile,
        SemVersion version,
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
            SurrealSchemaFile.Bearer => "bearer",
            _ => throw new NotImplementedException(),
        };

        bool hasVersionedSchemas = schemaFile switch
        {
            SurrealSchemaFile.User => true,
            _ => false,
        };

        string folder = hasVersionedSchemas ? $"Schemas/v{version.Major}" : "Schemas";

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            $"{folder}/{schemaFileName}.surql"
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
