using Microsoft.AspNetCore.Mvc.Testing;
using DocAssistant.Ai.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    public class SwaggerMemoryManagerServiceTest: IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly ISwaggerMemoryManagerService _swaggerMemoryManagerService;

        public SwaggerMemoryManagerServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
            factory.Services.GetRequiredService<MemoryServerless>();
            _swaggerMemoryManagerService = factory.Services.GetRequiredService<ISwaggerMemoryManagerService>();
        }

        [Fact]
        public async Task CanRemoveMemory()
        {
            await _swaggerMemoryManagerService.RemoveMemory();
        }

        [Fact]
        public async Task CanCreateContainerIfNotExist()
        {
            await _swaggerMemoryManagerService.CreateContainerIfNotExist();
        }

        [Fact]
        public async Task CanUploadMemory()
        {
            var fileName = "petstore-swagger-full.json";
            var apiToken = "YourApiToken";
            string path = $"Assets/{fileName}";

            await using var stream = File.OpenRead(path);

           await _swaggerMemoryManagerService.UploadMemory(fileName, stream, apiToken);
        }
    }
}
