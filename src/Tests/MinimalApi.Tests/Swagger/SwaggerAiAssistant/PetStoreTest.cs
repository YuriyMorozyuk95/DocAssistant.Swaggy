using System.Collections;
using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger.SwaggerAiAssistant;

public class PetStoreTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ISwaggerAiAssistantService _swaggerAiAssistantService;
    private string _swaggerFile;

    public PetStoreTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _swaggerAiAssistantService = factory.Services.GetRequiredService<ISwaggerAiAssistantService>();
    }

    public async Task InitializeAsync()
    {
        string swaggerFilePath = "Assets/petstore-swagger-full.json";
        _swaggerFile = await File.ReadAllTextAsync(swaggerFilePath);
    }

    [Fact]
    public async Task CanAskApiUpdate()
    {
        var userPrompt = "Update pet in store with id 10 to name Barsik, and make his status available?";
        var result = await _swaggerAiAssistantService.AskApi(_swaggerFile, userPrompt);

        PrintResult(result.FinalleResult, result.ToJson());
    }

    [Theory]
    [ClassData(typeof(UserPromptsTestData))]
    public async Task CanAskApi(string userPrompt)
    {
        var result = await _swaggerAiAssistantService.AskApi(_swaggerFile, userPrompt);

        PrintResult(result.FinalleResult, result.ToJson());
    }

    //TODO add more test cases
    [Fact]
    public async Task SummaryPrompt()
    {
        var input = "Find pet by id 2";
        var curl = "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/2\" -H \"accept: application/json\"";
        var response = "{\"id\":8,\"category\":{\"id\":4,\"name\":\"Lions\"},\"name\":\"Lion 2\",\"photoUrls\":[\"url1\",\"url2\"],\"tags\":[{\"id\":1,\"name\":\"tag2\"},{\"id\":2,\"name\":\"tag3\"}],\"status\":\"available\"}";

        var chatResult = await _swaggerAiAssistantService.SummarizeForNonTechnical(input, curl, response);

        PrintResult(chatResult.ToString(), chatResult.Metadata.ToJson());
    }

    [Theory]
    [ClassData(typeof(UserPromptsTestData))]
    public async Task GenerateCurl(string userPrompt)
    {
        var result = await _swaggerAiAssistantService.GenerateCurl(_swaggerFile, userPrompt);

        PrintResult(result.ToString(), result.Metadata.ToJson());
    }

    private void PrintResult(string content, string metadata)
    {
        _testOutputHelper.WriteLine("result: " + content);

        _testOutputHelper.WriteLine("metadata: " + metadata);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

public class UserPromptsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "Update an existing pet with id 1 to name doggie 1" };
        yield return new object[] { "Find pet by id 9223372036854761000" };
        yield return new object[] { "Returns pet inventories by status" };
        yield return new object[] { "Find purchase order by id 3" };
        yield return new object[] { "Find pet by id 9223372036854761000" };
        yield return new object[] { "Returns pet inventories by status" };  //ERROR
        yield return new object[] { "Find purchase order by id 6" };   //ERROR
        yield return new object[] { "Find pet by id 9223372036854761000" };  //ERROR
        yield return new object[] { "Returns pet inventories by status" };  //ERROR
        yield return new object[] { "Find purchase order by id 9223372036854761000" };  //ERROR
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


