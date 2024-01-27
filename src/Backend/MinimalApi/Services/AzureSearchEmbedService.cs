using System.Net;
using System.Text.RegularExpressions;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace MinimalApi.Services;

public interface IAzureSearchEmbedService
{
    Task<bool> EmbedBlob(Stream blobStream, string blobName, string searchIndexName, string embeddingModelName, Uri originDocUrl, string[] userGroups);

    Task CreateSearchIndex(string searchIndexName);
    Task EnsureSearchIndex(string searchIndexName);
    Task RemoveSearchIndex(string searchIndexName);
}

public sealed partial class AzureSearchAzureSearchEmbedService : IAzureSearchEmbedService
{
    private readonly OpenAIClient _openAiClient;
    private readonly SearchClient _indexSectionClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly DocumentAnalysisClient _documentAnalysisClient;
    private readonly ILogger<AzureSearchAzureSearchEmbedService> _logger;
    private readonly IStorageService _storageService;

    public AzureSearchAzureSearchEmbedService(OpenAIClient openAiClient,
        SearchClient indexSectionClient,
        SearchIndexClient searchIndexClient,
        DocumentAnalysisClient documentAnalysisClient,
        ILogger<AzureSearchAzureSearchEmbedService> logger,
        IStorageService storageService)
    {
        _openAiClient = openAiClient;
        _indexSectionClient = indexSectionClient;
        _searchIndexClient = searchIndexClient;
        _documentAnalysisClient = documentAnalysisClient;
        _logger = logger;
        _storageService = storageService;
    }

    [GeneratedRegex("[^0-9a-zA-Z_-]")]
    private static partial Regex MatchInSetRegex();

    public async Task<bool> EmbedBlob(Stream blobStream, string blobName, string searchIndexName, string embeddingModelName, Uri originDocUrl, string[] userGroups)
    {
        try
        {
            await EnsureSearchIndex(searchIndexName);
            Console.WriteLine($"Embedding blob '{blobName}'");
            var pageMap = await GetDocumentTextAsync(blobStream, blobName);

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

            // Create corpus from page map and upload to blob
            // Corpus name format: fileName-{page}.txt
            foreach (var page in pageMap)
            {
                var corpusName = $"{fileNameWithoutExtension}-{page.Index}.txt";
                await UploadCorpusAsync(corpusName, page.Text, originDocUrl, userGroups);
            }

            var sections = CreateSections(pageMap, blobName, originDocUrl.ToString(), userGroups);

            await IndexSectionsAsync(searchIndexName, sections, blobName, embeddingModelName);

            return true;
        }
        catch (Exception exception)
        {
            _logger?.LogError(
                exception, "Failed to embed blob '{BlobName}'", blobName);

            return false;
        }
    }

    public async Task CreateSearchIndex(string searchIndexName)
    {
        string vectorSearchConfigName = "my-vector-config";
        string vectorSearchProfile = "my-vector-profile";
        var index = new SearchIndex(searchIndexName)
        {
            VectorSearch = new()
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchConfigName)
                },
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchProfile, vectorSearchConfigName)
                }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SimpleField(IndexSection.UserGroupsFieldName, SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFacetable = true, IsFilterable = true, },
                new SimpleField("sourcepage", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcefile", SearchFieldDataType.String) { IsFacetable = true },
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    VectorSearchDimensions = 1536,
                    IsSearchable = true,
                    VectorSearchProfileName = vectorSearchProfile,
                }
            },
            SemanticSearch  = new ()
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new ()
                    {
                        ContentFields =
                        {
                            new SemanticField("content")
                        }
                    })
                }
            }
        };

       _logger?.LogInformation(
            "Creating '{searchIndexName}' search index", searchIndexName);

        await _searchIndexClient.CreateIndexAsync(index);
    }


    public async Task EnsureSearchIndex(string searchIndexName)
    {
        var indexNames = _searchIndexClient.GetIndexNamesAsync();
        await foreach (var page in indexNames.AsPages())
        {
            if (page.Values.Any(indexName => indexName == searchIndexName))
            {
                _logger?.LogWarning(
                    "Search index '{SearchIndexName}' already exists", searchIndexName);
                return;
            }
        }

        await CreateSearchIndex(searchIndexName);
    }

    public Task RemoveSearchIndex(string searchIndexName)
    {
        return _searchIndexClient.DeleteIndexAsync(searchIndexName);  
    }

    public async Task<IReadOnlyList<PageDetail>> GetDocumentTextAsync(Stream blobStream, string blobName)
    {
        _logger?.LogInformation(
            "Extracting text from '{Blob}' using Azure Form Recognizer", blobName);

        Console.WriteLine($"Extracting text from '{blobName}' using Azure Form Recognizer");
        using var ms = new MemoryStream();
        blobStream.CopyTo(ms);
        ms.Position = 0;
        AnalyzeDocumentOperation operation = _documentAnalysisClient.AnalyzeDocument(
            WaitUntil.Started, "prebuilt-layout", ms);

        var offset = 0;
        List<PageDetail> pageMap = new();

        var results = await operation.WaitForCompletionAsync();
        var pages = results.Value.Pages;
        for (var i = 0; i < pages.Count; i++)
        {
            IReadOnlyList<DocumentTable> tablesOnPage =
                results.Value.Tables.Where(t => t.BoundingRegions[0].PageNumber == i + 1).ToList();

            // Mark all positions of the table spans in the page
            int pageIndex = pages[i].Spans[0].Index;
            int pageLength = pages[i].Spans[0].Length;
            int[] tableChars = Enumerable.Repeat(-1, pageLength).ToArray();
            for (var tableId = 0; tableId < tablesOnPage.Count; tableId++)
            {
                foreach (DocumentSpan span in tablesOnPage[tableId].Spans)
                {
                    // Replace all table spans with "tableId" in tableChars array
                    for (var j = 0; j < span.Length; j++)
                    {
                        int index = span.Index - pageIndex + j;
                        if (index >= 0 && index < pageLength)
                        {
                            tableChars[index] = tableId;
                        }
                    }
                }
            }

            // Build page text by replacing characters in table spans with table HTML
            StringBuilder pageText = new();
            HashSet<int> addedTables = new();
            for (int j = 0; j < tableChars.Length; j++)
            {
                if (tableChars[j] == -1)
                {
                    pageText.Append(results.Value.Content[pageIndex + j]);
                }
                else if (!addedTables.Contains(tableChars[j]))
                {
                    pageText.Append(TableToHtml(tablesOnPage[tableChars[j]]));
                    addedTables.Add(tableChars[j]);
                }
            }

            pageText.Append(' ');
            pageMap.Add(new PageDetail(i, offset, pageText.ToString()));
            offset += pageText.Length;
        }
        Console.WriteLine($"Extracted {pageMap.Count} pages from '{blobName}'");
        return pageMap.AsReadOnly();
    }

    private static string TableToHtml(DocumentTable table)
    {
        var tableHtml = new StringBuilder("<table>");
        var rows = new List<DocumentTableCell>[table.RowCount];
        for (int i = 0; i < table.RowCount; i++)
        {
            rows[i] = table.Cells.Where(c => c.RowIndex == i).OrderBy(c => c.ColumnIndex).ToList();
        }

        foreach (var rowCells in rows)
        {
            tableHtml.Append("<tr>");
            foreach (DocumentTableCell cell in rowCells)
            {
                var tag = (cell.Kind == "columnHeader" || cell.Kind == "rowHeader") ? "th" : "td";
                var cellSpans = string.Empty;
                if (cell.ColumnSpan > 1)
                {
                    cellSpans += $" colSpan='{cell.ColumnSpan}'";
                }

                if (cell.RowSpan > 1)
                {
                    cellSpans += $" rowSpan='{cell.RowSpan}'";
                }

                tableHtml.AppendFormat(
                    "<{0}{1}>{2}</{0}>", tag, cellSpans, WebUtility.HtmlEncode(cell.Content));
            }

            tableHtml.Append("</tr>");
        }

        tableHtml.Append("</table>");

        return tableHtml.ToString();
    }

    private async Task UploadCorpusAsync(string corpusBlobName, string text, Uri originUri, string[] userGroups)
    {
        var container = await _storageService.GetOutputBlobContainerClient();
        var blobClient = container.GetBlobClient(corpusBlobName);
        if (await blobClient.ExistsAsync())
        {
            return;
        }

        _logger?.LogInformation("Uploading corpus '{CorpusBlobName}'", corpusBlobName);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = "text/plain"
        });

        // Set the metadata  
        var metadata = new Dictionary<string, string>
        {
            {"OriginUri", originUri.ToString()},
            {IndexSection.UserGroupsFieldName, string.Join(",", userGroups) },
        };

        await blobClient.SetMetadataAsync(metadata);
    }

    private IEnumerable<IndexSection> CreateSections(
        IReadOnlyList<PageDetail> pageMap, string blobName, string sourceFileUri, string[] userGroups)
    {
        const int maxSectionLength = 1_000;
        const int sentenceSearchLimit = 100;
        const int sectionOverlap = 100;

        var sentenceEndings = new[] { '.', '!', '?' };
        var wordBreaks = new[] { ',', ';', ':', ' ', '(', ')', '[', ']', '{', '}', '\t', '\n' };
        var allText = string.Concat(pageMap.Select(p => p.Text));
        var length = allText.Length;
        var start = 0;
        var end = length;

        _logger?.LogInformation("Splitting '{BlobName}' into sections", blobName);

        while (start + sectionOverlap < length)
        {
            var lastWord = -1;
            end = start + maxSectionLength;

            if (end > length)
            {
                end = length;
            }
            else
            {
                // Try to find the end of the sentence
                while (end < length && (end - start - maxSectionLength) < sentenceSearchLimit && !sentenceEndings.Contains(allText[end]))
                {
                    if (wordBreaks.Contains(allText[end]))
                    {
                        lastWord = end;
                    }
                    end++;
                }

                if (end < length && !sentenceEndings.Contains(allText[end]) && lastWord > 0)
                {
                    end = lastWord; // Fall back to at least keeping a whole word
                }
            }

            if (end < length)
            {
                end++;
            }

            // Try to find the start of the sentence or at least a whole word boundary
            lastWord = -1;
            while (start > 0 && start > end - maxSectionLength -
                (2 * sentenceSearchLimit) && !sentenceEndings.Contains(allText[start]))
            {
                if (wordBreaks.Contains(allText[start]))
                {
                    lastWord = start;
                }
                start--;
            }

            if (!sentenceEndings.Contains(allText[start]) && lastWord > 0)
            {
                start = lastWord;
            }
            if (start > 0)
            {
                start++;
            }

            var sectionText = allText[start..end];

            yield return new IndexSection(
                id: MatchInSetRegex().Replace($"{blobName}-{start}", "_").TrimStart('_'),
                content: sectionText,
                sourcePage: BlobNameFromFilePage(blobName),
                sourceFile: sourceFileUri,
                userGroups: userGroups);

            var lastTableStart = sectionText.LastIndexOf("<table", StringComparison.Ordinal);
            if (lastTableStart > 2 * sentenceSearchLimit && lastTableStart > sectionText.LastIndexOf("</table", StringComparison.Ordinal))
            {
                // If the section ends with an unclosed table, we need to start the next section with the table.
                // If table starts inside SentenceSearchLimit, we ignore it, as that will cause an infinite loop for tables longer than MaxSectionLength
                // If last table starts inside SectionOverlap, keep overlapping
                if (_logger?.IsEnabled(LogLevel.Warning) is true)
                {
                    _logger?.LogWarning("""
                        Section ends with unclosed table, starting next section with the
                        table at page {Offset} offset {Start} table start {LastTableStart}
                        """,
                        FindPage(pageMap, start),
                        start,
                        lastTableStart);
                }

                start = Math.Min(end - sectionOverlap, start + lastTableStart);
            }
            else
            {
                start = end - sectionOverlap;
            }
        }

        if (start + sectionOverlap < end)
        {
            yield return new IndexSection(
                id: MatchInSetRegex().Replace($"{blobName}-{start}", "_").TrimStart('_'),
                content: allText[start..end],
                sourcePage: BlobNameFromFilePage(blobName),
                sourceFile: sourceFileUri,
                userGroups: userGroups);
        }
    }

    private static int FindPage(IReadOnlyList<PageDetail> pageMap, int offset)
    {
        var length = pageMap.Count;
        for (var i = 0; i < length - 1; i++)
        {
            if (offset >= pageMap[i].Offset && offset < pageMap[i + 1].Offset)
            {
                return i;
            }
        }

        return length - 1;
    }

    private static string BlobNameFromFilePage(string blobName) => blobName;

    private async Task IndexSectionsAsync(string searchIndexName, IEnumerable<IndexSection> sections, string blobName, string embeddingModelName)
    {
        var infoLoggingEnabled = _logger?.IsEnabled(LogLevel.Information);
        if (infoLoggingEnabled is true)
        {
            _logger?.LogInformation("""
                Indexing sections from '{BlobName}' into search index '{SearchIndexName}'
                """,
                blobName,
                searchIndexName);
        }

        var iteration = 0;
        var batch = new IndexDocumentsBatch<SearchDocument>();
        foreach (var section in sections)
        {
            Response<Embeddings> embeddings;
            var options = new EmbeddingsOptions(embeddingModelName, new[] { section.Content.Replace('\r', ' ') });

            try
            {
                embeddings = await _openAiClient.GetEmbeddingsAsync(options);
                await Task.Delay(1000);
            }
            catch (Exception e)
            {
                await Task.Delay(3000);
                embeddings = await _openAiClient.GetEmbeddingsAsync(options);
                Console.WriteLine(e.Message);
            }

            var embedding = embeddings.Value.Data.FirstOrDefault()?.Embedding.ToArray() ?? Array.Empty<float>();
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = section.Id,
                    ["content"] = section.Content,
                    [IndexSection.UserGroupsFieldName] = section.UserGroups,
                    ["sourcepage"] = section.SourcePage,
                    ["sourcefile"] = section.SourceFile,
                    ["embedding"] = embedding,
                }));

            IndexCreationInformation.IndexCreationInfo.ChunksProcessed++;

            iteration++;
            if (iteration % 1_000 is 0)
            {
                // Every one thousand documents, batch create.
                IndexDocumentsResult result = await _indexSectionClient.IndexDocumentsAsync(batch);
                int succeeded = result.Results.Count(r => r.Succeeded);
                if (infoLoggingEnabled is true)
                {
                    _logger?.LogInformation("""
                        Indexed {Count} sections, {Succeeded} succeeded
                        """,
                        batch.Actions.Count,
                        succeeded);
                }

                batch = new();
            }
        }

        if (batch is { Actions.Count: > 0 })
        {
            // Any remaining documents, batch create.
            var index = new SearchIndex($"index-{batch.Actions.Count}");
            IndexDocumentsResult result = await _indexSectionClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            if (_logger?.IsEnabled(LogLevel.Information) is true)
            {
                _logger?.LogInformation("""
                    Indexed {Count} sections, {Succeeded} succeeded
                    """,
                batch.Actions.Count,
                succeeded);
            }
        }
    }
}
