using System.Collections;
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


