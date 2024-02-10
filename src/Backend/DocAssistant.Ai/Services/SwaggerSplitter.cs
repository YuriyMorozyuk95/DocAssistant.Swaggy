using System.Collections.Concurrent;
using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

using static Microsoft.KernelMemory.Citation;

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

        public static (string[] paths, string document, string apiKey) MergeSwagger(List<Partition> partitions)
        {
            if (!partitions.Any())
            {
                return (Array.Empty<string>(), string.Empty, string.Empty);
            }


            var firstPart = partitions.First();
            string apiKey = string.Empty;
            if(firstPart.Tags.TryGetValue(TagsKeys.ApiToken, out var tag))
            {
                apiKey = tag.FirstOrDefault();

            }
            var jsonDocuments = partitions.Select(x => x.Text).ToList();

            List<OpenApiDocument> documents = new List<OpenApiDocument>();

            foreach (var document in jsonDocuments)
            {
                var openApiDocument = new OpenApiStringReader().Read(document, out var diagnostic);
                if (openApiDocument == null)
                {
                    throw new ArgumentException();
                }
                documents.Add(openApiDocument);
            }

            var firstDocument = documents.First();

            var result = new OpenApiDocument
            {
                Info = firstDocument.Info,
                Servers = firstDocument.Servers,
                Paths = firstDocument.Paths,
                ExternalDocs = firstDocument.ExternalDocs,
                Extensions = firstDocument.Extensions,
                SecurityRequirements = firstDocument.SecurityRequirements,
                Tags = firstDocument.Tags,
                Workspace = firstDocument.Workspace,
            };

            foreach (var document in documents.Skip(1))
            {
                var path = document.Paths.First();
                result.Paths.Add(path.Key, path.Value);
            }

            var resultPaths = result.Paths.Select(x => x.Key).ToArray();
            return (resultPaths,  result.SerializeAsJson(OpenApiSpecVersion.OpenApi2_0), apiKey);
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
