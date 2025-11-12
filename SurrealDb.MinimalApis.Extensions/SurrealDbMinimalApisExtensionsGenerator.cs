using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SurrealDb.MinimalApis.Extensions;

[Generator]
public class SurrealDbMinimalApisExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context =>
        {
            var embeddedGeneratedFiles = new Dictionary<string, string>
            {
                {
                    "SurrealDb.MinimalApis.Extensions.SurrealDbMinimalApisExtensions.cs",
                    "SurrealDbMinimalApisExtensions.g.cs"
                },
                {
                    "SurrealDb.MinimalApis.Extensions.SurrealDbMinimalApisOptions.cs",
                    "SurrealDbMinimalApisOptions.g.cs"
                },
            };

            var assembly = Assembly.GetExecutingAssembly();

            foreach (var item in embeddedGeneratedFiles)
            {
                string resourceName = item.Key;
                string generatedFileName = item.Value;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                string generatedCode = reader.ReadToEnd();

                context.AddSource(generatedFileName, SourceText.From(generatedCode, Encoding.UTF8));
            }
        });
    }
}
