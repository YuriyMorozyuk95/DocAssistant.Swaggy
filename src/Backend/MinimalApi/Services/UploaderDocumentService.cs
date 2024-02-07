using Azure;
using DocAssistant.Ai;
using Newtonsoft.Json;

namespace MinimalApi.Services;

public interface IUploaderDocumentService
{
    //IAsyncEnumerable<DocumentResponse> GetDocuments(CancellationToken cancellationToken = default);
    //Task UploadToAzureIndex();
}
public class UploaderDocumentService : IUploaderDocumentService
{
    private readonly ILogger<UploaderDocumentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAzureSearchEmbedService _azureSearchEmbedService;
    private readonly IStorageService _storageService;

    public UploaderDocumentService(
        ILogger<UploaderDocumentService> logger,
        IConfiguration configuration,
        IAzureSearchEmbedService azureSearchEmbedService,
        IStorageService storageService)
    {
        _logger = logger;
        _configuration = configuration;
        _azureSearchEmbedService = azureSearchEmbedService;
        _storageService = storageService;
    }

    //public async IAsyncEnumerable<DocumentResponse> GetDocuments([EnumeratorCancellation] CancellationToken cancellationToken = default)
    //{
    //    var container = await _storageService.GetInputBlobContainerClient();
    //    await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
    //    {
    //        if (blob is not null and { Deleted: false })
    //        {
    //            var props = blob.Properties;
    //            var baseUri = container.Uri;
    //            var builder = new UriBuilder(baseUri);
    //            builder.Path += $"/{blob.Name}";

    //            BlobClient blobClient = container.GetBlobClient(blob.Name);
    //            Response<BlobProperties> response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
    //            IDictionary<string, string> metadata = response.Value.Metadata;

    //            var documentProcessingStatus = GetMetadataEnumOrDefault(
    //                metadata,
    //                nameof(DocumentProcessingStatus),
    //                DocumentProcessingStatus.NotProcessed);

    //            var embeddingType = GetMetadataEnumOrDefault(
    //                metadata,
    //                nameof(EmbeddingType),
    //                EmbeddingType.AzureSearch);

    //            string userGroupsString = string.Empty;
    //            if (metadata.TryGetValue("userGroups", out var userGroupsJson))
    //            {
    //                var userGroups = JsonConvert.DeserializeObject<UserGroup[]>(userGroupsJson);
    //                var userGroupsList = userGroups.Select(p => p.Name).ToArray();
    //                userGroupsString = string.Join(", ", userGroupsList);
    //            }

    //            yield return new DocumentResponse(
    //                blob.Name,
    //                props.ContentType,
    //                props.ContentLength ?? 0,
    //                props.LastModified,
    //                builder.Uri,
    //                documentProcessingStatus,
    //                embeddingType,
    //                userGroupsString);
    //        }
    //    }
    //}

    //public async Task UploadToAzureIndex()
    //{

    //    try
    //    {
    //        var searchIndexName = _configuration["AzureSearchIndex"];
    //        var embeddingModel = _configuration["AzureOpenAiEmbeddingDeployment"];

    //        _logger?.LogInformation("Deleting '{searchIndexName}' search index", searchIndexName);

    //        await _azureSearchEmbedService.RemoveSearchIndex(searchIndexName!);
    //        await _azureSearchEmbedService.CreateSearchIndex(searchIndexName!);

    //        var inputContainer = await _storageService.GetInputBlobContainerClient();
    //        var pageCount = await CalculateTotalDocumentsPagesAsync(inputContainer);
    //        IndexCreationInformation.IndexCreationInfo.Max = pageCount;

    //        await foreach (var document in GetDocuments())
    //        {

    //            var fileName = document.Name;
    //            var blobClient = inputContainer.GetBlobClient(document.Name);

    //            var stream = await GetBlobStreamAsync(blobClient);
    //            if (!stream.CanRead || !stream.CanSeek)
    //            {
    //                throw new NotSupportedException("The stream must be readable and seekable.");
    //            }

    //            string[] userGroupsList = Array.Empty<string>();
    //            //Deserialize the userGroups metadata to a list of strings
    //            BlobProperties properties = await blobClient.GetPropertiesAsync();
    //            var metadata = properties.Metadata;


    //            if (metadata.TryGetValue(IndexSection.UserGroupsFieldName, out string userGroupsJson))
    //            {
    //                var userGroups = JsonConvert.DeserializeObject<UserGroup[]>(userGroupsJson);
    //                userGroupsList = userGroups.Select(p => p.Name).ToArray();
    //            }

    //            using var documents = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

    //            for (int i = 0; i < documents.PageCount; i++)
    //            {
    //                IndexCreationInformation.IndexCreationInfo.Value++;

    //                var chunkName = BlobNameFromFilePage(fileName, i);
    //                var tempFileName = Path.GetTempFileName();

    //                try
    //                {
    //                    using var pdfDocument = new PdfDocument();
    //                    pdfDocument.AddPage(documents.Pages[i]);
    //                    pdfDocument.Save(tempFileName);
    //                    await using var tempStream = File.OpenRead(tempFileName);

    //                    await _azureSearchEmbedService.EmbedBlob(tempStream, chunkName, searchIndexName!, embeddingModel!, document.Url, userGroupsList);
    //                }
    //                catch (Exception ex)
    //                {
    //                    IndexCreationInformation.IndexCreationInfo.LastIndexErrorMessage = ex.Message;
    //                    IndexCreationInformation.IndexCreationInfo.LastIndexStatus = IndexStatus.Failed;
    //                }
    //                finally
    //                {
    //                    File.Delete(tempFileName);
    //                }
    //            }

    //            metadata[nameof(DocumentProcessingStatus)] = DocumentProcessingStatus.Succeeded.ToString();
    //            metadata[nameof(EmbeddingType)] = EmbeddingType.AzureSearch.ToString();

    //            await blobClient.SetMetadataAsync(metadata);

    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        IndexCreationInformation.IndexCreationInfo.LastIndexErrorMessage = ex.Message;
    //        IndexCreationInformation.IndexCreationInfo.LastIndexStatus = IndexStatus.Failed;
    //    }

    //    IndexCreationInformation.IndexCreationInfo.LastIndexStatus = IndexStatus.Succeeded;
    //}

    //private async Task<int> CalculateTotalDocumentsPagesAsync(BlobContainerClient containerClient)
    //{
    //    int counter = 0;
    //    await foreach (var doc in GetDocuments())
    //    {
    //        var blobClient = containerClient.GetBlobClient(doc.Name);

    //        var stream = await GetBlobStreamAsync(blobClient);

    //        using var documents = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    //        counter += documents.PageCount;
    //    }

    //    return counter;
    //}


    private async Task<Stream> GetBlobStreamAsync(BlobClient blobClient)
    {
        if (await blobClient.ExistsAsync())
        {
            BlobDownloadInfo download = await blobClient.DownloadAsync();

            var memoryStream = new MemoryStream();
            await download.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
        else
        {
            throw new FileNotFoundException("The blob does not exist.");
        }
    }

    private TEnum GetMetadataEnumOrDefault<TEnum>(
        IDictionary<string, string> metadata,
        string key,
        TEnum @default) where TEnum : struct
    {
        return metadata.TryGetValue(key, out var value)
               && Enum.TryParse<TEnum>(value, out var status)
            ? status
            : @default;
    }

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);
}