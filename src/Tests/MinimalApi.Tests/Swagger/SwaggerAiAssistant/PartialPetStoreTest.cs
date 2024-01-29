using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger.SwaggerAiAssistant;

public class PartialPetStoreTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ISwaggerAiAssistantService _swaggerAiAssistantService;

    public PartialPetStoreTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _swaggerAiAssistantService = factory.Services.GetRequiredService<ISwaggerAiAssistantService>();
    }

    [Fact]
    public async Task CanAskApiCreateOrder()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-order-create.json");

        var userPrompt = "Could you make an order for a pet with id 198773 with quantity 10?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalleResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiFindById()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-order-find-by-id.json");

        var userPrompt = "Could you find order by id 10?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalleResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiStoreInventory()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-order-inventories.json");

        var userPrompt = "Could you provide to me store inventories?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalleResult, result.ToJson());
    }

    [Fact]
    public async Task CanAskApiCreate()
    {
        var swaggerFile = await ReadSwagger("petstore-swagger-create-user.json");

        var userPrompt = "Could you create new user Alexander Whatson with email Alexander.Whatson@gmail.com with id 1000 ?";
        var result = await _swaggerAiAssistantService.AskApi(swaggerFile, userPrompt);

        PrintResult(result.FinalleResult, result.ToJson());
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


