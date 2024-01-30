using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;

namespace MinimalApi.Tests.Swagger
{
    public class SearchEndpointTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly MemoryServerless _memory;

        public SearchEndpointTest(WebApplicationFactory<Program> factory)
        {
            var config = factory.Services.GetRequiredService<IConfiguration>();

#pragma warning disable SKEXP0001
#pragma warning restore SKEXP0001

            var azureOpenAITextConfig = new AzureOpenAIConfig();
            var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
            var searchClientConfig = new SearchClientConfig();

            config.BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig);
            config.BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);
            config.BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);

            _memory = new KernelMemoryBuilder()
                .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
                .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
                .Build<MemoryServerless>();

        }

        public async Task InitializeAsync()
        {
            await _memory.ImportDocumentAsync("Assets/petstore-swagger-create-user.json");
            await _memory.ImportDocumentAsync("Assets/petstore-swagger-order-create.json");
            await _memory.ImportDocumentAsync("Assets/petstore-swagger-order-find-by-id.json");
            await _memory.ImportDocumentAsync("Assets/petstore-swagger-order-inventories.json");
        }

        [Fact]
        public async Task CanSearch()
        {
            var a = await _memory.SearchAsync("Could you make an order for a pet with id 198773 with quantity 10?");
        }

        

        public Task DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
