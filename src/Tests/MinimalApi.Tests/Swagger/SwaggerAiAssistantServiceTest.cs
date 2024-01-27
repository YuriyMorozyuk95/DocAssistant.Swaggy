using System.Collections;
using Blazor.Serialization.Extensions;

using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger;

public class SwaggerAiAssistantServiceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ISwaggerAiAssistantService _swaggerAiAssistantService;

    public SwaggerAiAssistantServiceTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _swaggerAiAssistantService = factory.Services.GetRequiredService<ISwaggerAiAssistantService>();
    }

    [Theory]
    [ClassData(typeof(UserPromptsTestData))]
    public async Task CanAskApi(string userPrompt)
    {
        var result = await _swaggerAiAssistantService.AskApi(userPrompt);

        PrintResult(result.ToString(), result.Metadata);
    }

    //TODO add more test cases
    [Fact]
    public async Task SummaryPrompt()
    {
        var input = "Find pet by id 2";
        var curl = "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/2\" -H \"accept: application/json\"";
        var response = "{\"id\":8,\"category\":{\"id\":4,\"name\":\"Lions\"},\"name\":\"Lion 2\",\"photoUrls\":[\"url1\",\"url2\"],\"tags\":[{\"id\":1,\"name\":\"tag2\"},{\"id\":2,\"name\":\"tag3\"}],\"status\":\"available\"}";

        var chatResult = await _swaggerAiAssistantService.SummarizeForNonTechnical(input, curl, response);

        PrintResult(chatResult.ToString(), chatResult.Metadata);
    }

    [Theory]
    [ClassData(typeof(UserPromptsTestData))]
    public async Task GenerateCurl(string userPrompt)
    {
        var result = await _swaggerAiAssistantService.GenerateCurl(userPrompt);

        PrintResult(result.ToString(), result.Metadata);
    }

    private void PrintResult(string content, IReadOnlyDictionary<string, object> metadata)
    {
        _testOutputHelper.WriteLine("result: " + content);

        var metadataJson = metadata.ToJson();
        _testOutputHelper.WriteLine("metadata: " + metadataJson);
    }
}

public class UserPromptsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "Update an existing pet with id 1 to name doggie 1" };
        yield return new object[] { "Find pet by id 2" };
        yield return new object[] { "Returns pet inventories by status" };
        yield return new object[] { "Find purchase order by id 3" };
        yield return new object[] { "Find pet by id 5" };
        yield return new object[] { "Returns pet inventories by status" };  //ERROR
        yield return new object[] { "Find purchase order by id 6" };   //ERROR
        yield return new object[] { "Find pet by id 8" };  //ERROR
        yield return new object[] { "Returns pet inventories by status" };  //ERROR
        yield return new object[] { "Find purchase order by id 9" };  //ERROR
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


