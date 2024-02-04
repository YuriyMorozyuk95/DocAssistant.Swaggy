﻿using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace DocAssistant.Ai.Services
{
    public class SwaggerSplitter
    {
        public IEnumerable<(string, string)> SplitJson(string swaggerFileText)
        {
            var openApiDocument = new OpenApiStringReader().Read(swaggerFileText, out var diagnostic);
            if (openApiDocument == null)
            {
                throw new ArgumentException();
            }

            foreach (var path in openApiDocument.Paths)
            {
                var document = new OpenApiDocument
                {
                    Info = openApiDocument.Info,
                    Servers = openApiDocument.Servers,
                    Paths = new OpenApiPaths { {path.Key, path.Value} },
                    ExternalDocs = openApiDocument.ExternalDocs,
                    Extensions = openApiDocument.Extensions,
                    SecurityRequirements = openApiDocument.SecurityRequirements,
                    Tags = openApiDocument.Tags,
                    Workspace = openApiDocument.Workspace,
                    //Components = openApiDocument.Components,
                };

                var refs = path.Value.Operations.Values.SelectMany(op =>
                {
                    return
                    op.RequestBody.Content.Values.Select(c =>
                    {
                        if (c.Schema.Reference != null)
                        {
                            return c.Schema.Reference;
                        }

                        return null;
                    });
                }).Where(x => x != null);

                yield return (path.Key, ReadToEnd(document));

                //yield return document.Serialize(OpenApiSpecVersion.OpenApi2_0, OpenApiFormat.Json);
            }
        }

        private static string ReadToEnd(OpenApiDocument document)
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
