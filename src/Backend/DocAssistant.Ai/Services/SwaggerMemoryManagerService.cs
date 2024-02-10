using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

namespace DocAssistant.Ai.Services;

public interface ISwaggerMemoryManagerService
{
    Task UploadMemory(string fileName, Stream stream, string apiKey);
    Task RemoveMemory();
    Task CreateContainerIfNotExist();
}

public class SwaggerMemoryManagerService : ISwaggerMemoryManagerService
{
    private readonly MemoryServerless _memory;
    private readonly IConfiguration _configuration;

    public SwaggerMemoryManagerService(MemoryServerless memory, IConfiguration configuration)
    {
        _memory = memory;
        _configuration = configuration;
    }

    public async Task UploadMemory(string fileName, Stream stream, string apiKey)
    {
        IndexCreationInformation.IndexCreationInfo.LastIndexStatus = Shared.IndexStatus.Processing;
        IndexCreationInformation.IndexCreationInfo.LastIndexErrorMessage = string.Empty;

        await CreateContainerIfNotExist();

        try
        {
            var tags = new TagCollection
        {
            { TagsKeys.SwaggerFile, fileName },
            { TagsKeys.ApiToken, apiKey }
        };

            var document = new Document(Guid.NewGuid().ToString(), tags);
            document.AddStream(fileName, stream);

            await _memory.ImportDocumentAsync(document);

            IndexCreationInformation.IndexCreationInfo.LastIndexStatus = Shared.IndexStatus.Succeeded;
        }
        catch (Exception e)
        {
            IndexCreationInformation.IndexCreationInfo.LastIndexStatus = Shared.IndexStatus.Failed;
            IndexCreationInformation.IndexCreationInfo.LastIndexErrorMessage = e.ToString();
        }
    }

    public async Task CreateContainerIfNotExist()
    {
        var connectionString = _configuration["KernelMemory:Services:AzureBlobs:ConnectionString"];
        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

        // Create the container client.    
        var containerName = _configuration["KernelMemory:Services:AzureBlobs:Container"];
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Create the container if it does not exist.  
        if (!await containerClient.ExistsAsync())
        {
            await containerClient.CreateIfNotExistsAsync();
        }

        // Set the access level of the container to Container (anonymous read access for containers and blobs).  
        await containerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);
    }

    public async Task RemoveMemory()
    {
        // Create a BlobServiceClient object which will be used to create a container client  
        var connectionString = _configuration["KernelMemory:Services:AzureBlobs:ConnectionString"];
        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
        // Create the container client.  
        var containerName = _configuration["KernelMemory:Services:AzureBlobs:Container"];
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        // Delete the container  
        await containerClient.DeleteIfExistsAsync();

        var apiKey = _configuration["KernelMemory:Services:AzureAISearch:APIKey"];
        var endpoint = _configuration["KernelMemory:Services:AzureAISearch:Endpoint"];
        SearchIndexClient indexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        var indexes = await _memory.ListIndexesAsync();
        foreach (var index in indexes)
        {
            await indexClient.DeleteIndexAsync(index.Name);
        }
    }
}