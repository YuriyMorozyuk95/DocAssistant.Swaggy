using System.Collections;
using Azure;
using Azure.AI.OpenAI;

using Blazor.Serialization.Extensions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger;

public class GenerateCurlTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IChatCompletion _chatService;

    public GenerateCurlTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var client = factory.Services.GetRequiredService<OpenAIClient>();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
        var kernel = Kernel.Builder
            .WithAzureChatCompletionService(deployedModelName, client)
            .Build();

        _chatService = kernel.GetService<IChatCompletion>();

    }

    [Theory]  
    [ClassData(typeof(UserPromptsTestData))]
    public async Task GenerateCurl(string userPrompt)
    {
        var systemPrompt = await GenerateSystemPrompt();

        var getQueryChat = _chatService.CreateNewChat(systemPrompt);

        getQueryChat.AddUserMessage(userPrompt);
        var result = await _chatService.GetChatCompletionsAsync(
            getQueryChat);

        PrintResult(result);
    }

    //public async Task CallGenerated(string userPrompt)
    //{
    //    var systemPrompt = await GenerateSystemPrompt();

    //    var getQueryChat = _chatService.CreateNewChat(systemPrompt);

    //    getQueryChat.AddUserMessage(userPrompt);
    //    var result = await _chatService.GetChatCompletionsAsync(
    //        getQueryChat);

    //    PrintResult(result);
    //}

    private static async Task<string> GenerateSystemPrompt()
    {
        string swaggerFilePath = "Assets/petstore-swagger-full.json";
        string swaggerPromptFilePath = "Assets/system-message-swagger.txt";

        var swaggerFile = await File.ReadAllTextAsync(swaggerFilePath, default(CancellationToken));
        var swaggerPrompt = await File.ReadAllTextAsync(swaggerPromptFilePath, default(CancellationToken));

        var systemPrompt = swaggerPrompt.Replace("{{swagger-file}}", swaggerFile);
        return systemPrompt;
    }

    private void PrintResult(IReadOnlyList<IChatResult> result)
    {
        var textResult = result[0].ModelResult.GetOpenAIChatResult().Choice.Message.Content;
        _testOutputHelper.WriteLine("result: " + textResult);

        var usage = result[0].ModelResult.GetOpenAIChatResult().Usage.ToJson();
        _testOutputHelper.WriteLine("usage: " + usage);
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
        yield return new object[] { "Returns pet inventories by status" };  
        yield return new object[] { "Find purchase order by id 6" };  
        yield return new object[] { "Find pet by id 8" };  
        yield return new object[] { "Returns pet inventories by status" };  
        yield return new object[] { "Find purchase order by id 9" };  
    }  
  
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();  
}  
