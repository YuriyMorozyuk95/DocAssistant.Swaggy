using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs.Models;
using Shared.Models;

namespace DocAssistant.Ai.Services
{
    public interface IDocumentStorageService
    {
        Task SetOriginFlagMetadata(string documentId, string documentName);
        IAsyncEnumerable<DocumentResponse> RetrieveOriginFiles();
    }

    public class DocumentStorageService : IDocumentStorageService
    {
        private readonly string? _connectionString;
        private readonly string? _containerName;

        public DocumentStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["KernelMemory:Services:AzureBlobs:ConnectionString"];
            _containerName = configuration["KernelMemory:Services:AzureBlobs:Container"];
        }

        public async Task SetOriginFlagMetadata(string documentId, string documentName)
        {
            var relativePath = $"default/{documentId}/{documentName}";

            // Create a BlobServiceClient object which will be used to create a container client  
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

            // Create the container and return a container client object  
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Get a reference to a blob  
            BlobClient blobClient = containerClient.GetBlobClient(relativePath);

            var metadata = new Dictionary<string, string> { { "isOriginFile", "true" } };
            await blobClient.SetMetadataAsync(metadata);
        }

        public async IAsyncEnumerable<DocumentResponse> RetrieveOriginFiles()
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);  
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);  
  
// Get all blobs in the container  
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(BlobTraits.Metadata))  
            {  
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);  
                var response = await blobClient.GetPropertiesAsync();  


                if (response.Value.Metadata.TryGetValue("isOriginFile", out string isOriginFile) && isOriginFile == "true")  
                {
                    string name = blobItem.Name.Split('/').Last();

                    yield return new DocumentResponse(
                        name,
                        blobItem.Properties.ContentType,
                        blobItem.Properties.ContentLength ?? 0,
                        blobItem.Properties.LastModified);
                }  
            }
        }
    }
}
