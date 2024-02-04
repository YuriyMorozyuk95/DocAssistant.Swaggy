using System.Diagnostics;
using DocAssistant.Ai;
using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Shared.Extensions;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    public class SwaggerMemorySearchServiceTest : IClassFixture<WebApplicationFactory<Program>>
    {
	    private readonly ITestOutputHelper _testOutputHelper;
	    private readonly MemoryServerless _memory;
	    private readonly ISwaggerAiAssistantService _swaggerAiAssistantService;
	    private readonly ISwaggerMemorySearchService _swaggerMemorySearchService;

	    public SwaggerMemorySearchServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
	        _testOutputHelper = testOutputHelper;
	        _memory = factory.Services.GetRequiredService<MemoryServerless>();
	        _swaggerAiAssistantService = factory.Services.GetRequiredService<ISwaggerAiAssistantService>();
	        _swaggerMemorySearchService = factory.Services.GetRequiredService<ISwaggerMemorySearchService>();
        }

	    [Fact]
	    public async Task CanAnswer()
	    {
		    var question = "Could you make an order for a pet with id 198773 with quantity 10?";
		   
		    var result = await _swaggerAiAssistantService.AskApi(question);
		    PrintResult(result.FinalResult, result.ToJson());
	    }

	    [Fact]
	    public async Task CanSearchDocument()
	    {
		    var question = "Could you make an order for a pet with id 198773 with quantity 10?";
		    
			var model = await _swaggerMemorySearchService.SearchDocument(question);

			_testOutputHelper.WriteLine(model.ToJson());
	    }

        [Fact]
        public async Task CanUploadDocuments()
        {
	        var output = await UploadDocuments(Guid.NewGuid().ToString());

	        _testOutputHelper.WriteLine(output);
		}


        [Fact]
        public async Task CanCreateAndRemoveFiles()
        {
           var guid = Guid.NewGuid().ToString();
           var output = await UploadDocuments(guid);

            _testOutputHelper.WriteLine(output);

            await _memory.DeleteDocumentAsync(guid);
        }

		private async Task<string> UploadDocuments(string guid)
        {
			var tags = new TagCollection
            {
                { TagsKeys.SwaggerFile, "petstore-swagger-full.json" }
            };
            string path = "Assets/PetStore";  

            string[] files = Directory.GetFiles(path);  
            var upload = new Document(guid, tags, files);

	        var output = await _memory.ImportDocumentAsync(upload);
	        return output;
        }

        private void PrintResult(string content, string metadata)
        {
	        _testOutputHelper.WriteLine("result: " + content);

	        _testOutputHelper.WriteLine("metadata: " + metadata);
        }
    }
}
