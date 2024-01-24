namespace MinimalApi.Services;

public interface IStorageService
{
    Task<BlobContainerClient> GetInputBlobContainerClient();
    Task<BlobContainerClient> GetOutputBlobContainerClient();
}

public class StorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;
    private BlobContainerClient _inputBlobContainerClient = null;
    private BlobContainerClient _outputBlobContainerClient = null;

    public StorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _configuration = configuration;
    }

    public async Task<BlobContainerClient> GetInputBlobContainerClient()
    {
        return _inputBlobContainerClient ??= await CreateBlobContainerClientAsync(_configuration["InputAzureStorageContainer"]!);
    }

    public async Task<BlobContainerClient> GetOutputBlobContainerClient()
    {
        return _outputBlobContainerClient ??= await CreateBlobContainerClientAsync(_configuration["OutputAzureStorageContainer"]!);
    }

    private async Task<BlobContainerClient> CreateBlobContainerClientAsync(string containerName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();

        return container;
    }
}
