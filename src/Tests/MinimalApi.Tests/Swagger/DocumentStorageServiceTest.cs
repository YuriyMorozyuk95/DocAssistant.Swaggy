using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    public class DocumentStorageServiceTest : IClassFixture<WebApplicationFactory<Program>>
    {
	    private readonly ITestOutputHelper _testOutputHelper;
        private readonly IConfiguration _configuration;

        public DocumentStorageServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
	        _testOutputHelper = testOutputHelper;
	        _configuration = factory.Services.GetRequiredService<IConfiguration>();
        }

        [Fact]
        public async Task CanUpdateMetadata()
        {
            var connectionString = _configuration["KernelMemory:Services:AzureBlobs:ConnectionString"];
            var containerName = _configuration["KernelMemory:Services:AzureBlobs:Container"];

            // Create a BlobServiceClient object which will be used to create a container client  
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            // Create the container and return a container client object  
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Get a reference to a blob  

            string blobUrl = "default/0137e94d-253d-4347-b3af-f38cafbad80f/Test1.json";

            BlobClient blobClient = containerClient.GetBlobClient(blobUrl);

            var metadata = new Dictionary<string, string> { { "isOriginFile", "true" } };
            await blobClient.SetMetadataAsync(metadata);
        }

        [Fact]
        public async Task CanRetrieveOriginFiles()
        {
            var connectionString = _configuration["KernelMemory:Services:AzureBlobs:ConnectionString"];
            var containerName = _configuration["KernelMemory:Services:AzureBlobs:Container"];  
  
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);  
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);  
  
// Get all blobs in the container  
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(BlobTraits.Metadata))  
            {  
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);  
                var response = await blobClient.GetPropertiesAsync();  
                if (response.Value.Metadata.TryGetValue("isOriginFile", out string isOriginFile) && isOriginFile == "true")  
                {  
                    _testOutputHelper.WriteLine(blobItem.Name);  
                }  
            }  

        }
    }
}
