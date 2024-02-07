using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    public class DocumentStorageServiceTest : IClassFixture<WebApplicationFactory<Program>>
    {
	    private readonly ITestOutputHelper _testOutputHelper;
        private readonly IDocumentStorageService _documentStorageService;

        public DocumentStorageServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
	        _testOutputHelper = testOutputHelper;
	        _documentStorageService = factory.Services.GetRequiredService<IDocumentStorageService>();
        }

        [Fact]
        public async Task CanUpdateMetadata()
        {
            await _documentStorageService.SetOriginFlagMetadata("0137e94d-253d-4347-b3af-f38cafbad80f", "Test1.json");
        }

        [Fact]
        public async Task CanRetrieveOriginFiles()
        {
            await foreach (var blobItem in _documentStorageService.RetrieveOriginFiles())
            {
                _testOutputHelper.WriteLine(blobItem.Name);
            }
        }
    }
}
