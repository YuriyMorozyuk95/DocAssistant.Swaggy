using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace DocAssistant.Ai.Services
{
    public static class SwaggerSplitter
    {
        public static IEnumerable<(string path, string document)> SplitSwagger(string swaggerFileText, IProgress<(int max, int value)> progress)
        {
            var openApiDocument = new OpenApiStringReader().Read(swaggerFileText, out var diagnostic);
            if (openApiDocument == null)
            {
                throw new ArgumentException();
            }

            var total = openApiDocument.Paths.Count;
            var current = 0;
            foreach (var path in openApiDocument.Paths)
            {
                progress.Report((total, current++));
                var document = new OpenApiDocument
                {
                    Info = openApiDocument.Info,
                    Servers = openApiDocument.Servers,
                    Paths = new OpenApiPaths { { path.Key, path.Value } },
                    ExternalDocs = openApiDocument.ExternalDocs,
                    Extensions = openApiDocument.Extensions,
                    SecurityRequirements = openApiDocument.SecurityRequirements,
                    Tags = openApiDocument.Tags,
                    Workspace = openApiDocument.Workspace,
                };

                yield return (path.Key, Serialize(document));
            }
        }

        private static string Serialize(OpenApiDocument document)
        {
            var writerSettings = new OpenApiWriterSettings() { InlineLocalReferences = true, InlineExternalReferences = true };

            var ms = new MemoryStream();
            using (var streamWriter = new StreamWriter(ms, Encoding.Default, 1024, true))
            {
                var writer = new OpenApiJsonWriter(streamWriter, writerSettings);
                document.SerializeAsV2(writer);
            }

            ms.Position = 0;
            using var streamReader = new StreamReader(ms);
            var result = streamReader.ReadToEnd();

            return result;
        }
    }
}
