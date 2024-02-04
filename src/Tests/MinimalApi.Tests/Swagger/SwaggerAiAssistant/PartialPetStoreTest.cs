using DocAssistant.Ai;
using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using MinimalApi.Tests.Swagger.SwaggerAiAssistant.UserPromptsTestData;
using Shared.Extensions;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger.SwaggerAiAssistant;

public class PartialPetStoreTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly MemoryServerless _memoryServerless;
    private readonly ISwaggerAiAssistantService _swaggerAiAssistantService;

    public PartialPetStoreTest(
        WebApplicationFactory<Program> factory,
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _swaggerAiAssistantService = factory.Services.GetRequiredService<ISwaggerAiAssistantService>();
        _memoryServerless = factory.Services.GetRequiredService<MemoryServerless>();
    }
    [Fact]
    private async Task UploadPetStoreSwagger()
    {
        var tags = new TagCollection
        {
            { TagsKeys.SwaggerFile, "petstore-swagger-full.json" }
        };
        string path = "Assets/PetStore";  

        string[] files = Directory.GetFiles(path);  
        var upload = new Document(Guid.NewGuid().ToString(), tags, files);

        await _memoryServerless.ImportDocumentAsync(upload);
    }

    //Please call UploadPetStoreSwagger, before running this test
    [Theory]
    [ClassData(typeof(PetStoreUserPromptsTestData))]
    public async Task CanAskApi(string userPrompt)
    {
        var result = await _swaggerAiAssistantService.AskApi(userPrompt);

        PrintResult(result.FinalResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiCreateOrder()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-order-create.json");

        var userPrompt = "Could you make an order for a pet with id 198773 with quantity 10?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiFindById()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-order-find-by-id.json");

        var userPrompt = "Could you find order by id 10?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiStoreInventory()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-order-inventories.json");

        var userPrompt = "Could you provide to me store inventories?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiCreate()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-create-user.json");

        var userPrompt = "Could you create new user Alexander Whatson with email Alexander.Whatson@gmail.com with id 1000 ?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalResult, result.ToJson());
    }

    private Task<string> ReadSwagger(string fileName)
    {
        string swaggerFilePath = $"Assets/{fileName}";
        return File.ReadAllTextAsync(swaggerFilePath);
    }

    private void PrintResult(string content, string metadata)
    {
        _testOutputHelper.WriteLine("result: " + content);

        _testOutputHelper.WriteLine("metadata: " + metadata);
    }
}


