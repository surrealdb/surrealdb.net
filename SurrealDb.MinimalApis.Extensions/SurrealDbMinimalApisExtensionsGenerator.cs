using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SurrealDb.MinimalApis.Extensions;

[Generator]
public class SurrealDbMinimalApisExtensionsGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();

        {
            string resourceName =
                "SurrealDb.MinimalApis.Extensions.SurrealDbMinimalApisExtensions.cs";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            string generatedCode = reader.ReadToEnd();

            context.AddSource(
                "SurrealDbMinimalApisExtensions.g.cs",
                SourceText.From(generatedCode, Encoding.UTF8)
            );
        }

        {
            string resourceName = "SurrealDb.MinimalApis.Extensions.SurrealDbMinimalApisOptions.cs";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            string generatedCode = reader.ReadToEnd();

            context.AddSource(
                "SurrealDbMinimalApisOptions.g.cs",
                SourceText.From(generatedCode, Encoding.UTF8)
            );
        }
    }

    public void Initialize(GeneratorInitializationContext context) { }
}
