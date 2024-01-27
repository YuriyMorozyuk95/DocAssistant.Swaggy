using System.Collections;
using System.Diagnostics;

using Azure.AI.OpenAI;

using Blazor.Serialization.Extensions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger;

public class GenerateCurlTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IChatCompletionService _chatService;
    private readonly Kernel _kernel;

    public GenerateCurlTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var client = factory.Services.GetRequiredService<OpenAIClient>();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
        _kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployedModelName, client)
            .AddAzureOpenAITextGeneration(deployedModelName, client)
            .Build();

        _chatService = _kernel.GetRequiredService<IChatCompletionService>();

    }

    [Fact]
    public async Task SummaryPrompt()
    {
        var input = "Find pet by id 2";
        var curl = "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/2\" -H \"accept: application/json\"";
        var response = "{\"id\":8,\"category\":{\"id\":4,\"name\":\"Lions\"},\"name\":\"Lion 2\",\"photoUrls\":[\"url1\",\"url2\"],\"tags\":[{\"id\":1,\"name\":\"tag2\"},{\"id\":2,\"name\":\"tag3\"}],\"status\":\"available\"}";

        var prompts = _kernel.ImportPluginFromPromptDirectory("Prompts");

        var summaryPrompt = prompts["SummarizeForNonTechnical"];
        // Get chat response  
        var chatResult = await _kernel.InvokeAsync(
            summaryPrompt,
                new KernelArguments() {
                    { "input", input },
                    { "curl", curl },
                    { "response", response }
                }
            );

        _testOutputHelper.WriteLine("chatResult: " + chatResult);
    }

    [Theory]
    [ClassData(typeof(UserPromptsTestData))]
    public async Task GenerateCurl(string userPrompt)
    {
        var systemPrompt = await GenerateSystemPrompt();

        var getQueryChat = new ChatHistory(systemPrompt);
        getQueryChat.AddUserMessage(userPrompt);

        var result = await _chatService.GetChatMessageContentsAsync(getQueryChat);

        PrintResult(result);
    }

    [Theory]
    [ClassData(typeof(CurlTestData))]
    public async Task CallCurl(string curl)
    {
        var response = await ExecuteCurl(curl);
        _testOutputHelper.WriteLine("response: " + response);
    }

    private async Task<string> ExecuteCurl(string curl)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = "/C" + curl,
            RedirectStandardOutput = true
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Process process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true };

        process.Start();

        try
        {
            await process.WaitForExitAsync(cts.Token);
            string result = await process.StandardOutput.ReadToEndAsync(cts.Token);

            return result;
        }
        catch (TaskCanceledException)
        {
            process.Kill();
            return "Process timed out and was terminated";
        }
    }

    private static async Task<string> GenerateSystemPrompt()
    {
        string swaggerFilePath = "Assets/petstore-swagger-full.json";
        string swaggerPromptFilePath = "Assets/system-prompt-swagger.txt";

        var swaggerFile = await File.ReadAllTextAsync(swaggerFilePath, default(CancellationToken));
        var swaggerPrompt = await File.ReadAllTextAsync(swaggerPromptFilePath, default(CancellationToken));

        var systemPrompt = swaggerPrompt.Replace("{{swagger-file}}", swaggerFile);
        return systemPrompt;
    }

    private static async Task<string> GenerateSummaryPrompt(string input, string curl, string response)
    {
        string summaryFilePath = "Assets/summary-prompt-swagger.txt";

        var summaryPrompt = await File.ReadAllTextAsync(summaryFilePath, default(CancellationToken));

        var systemPrompt = summaryPrompt.Replace("{{input}}", input);
        systemPrompt = systemPrompt.Replace("{{curl}}", input);
        systemPrompt = systemPrompt.Replace("{{response}}", input);

        return systemPrompt;
    }

    private void PrintResult(IReadOnlyList<ChatMessageContent> result)
    {
        var textResult = result[0].Content;
        _testOutputHelper.WriteLine("result: " + textResult);

        var usage = result[0].Metadata.ToJson();
        _testOutputHelper.WriteLine("usage: " + usage);
    }
}

public class UserPromptsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "Update an existing pet with id 1 to name doggie 1" };
        //yield return new object[] { "Find pet by id 2" };  
        //yield return new object[] { "Returns pet inventories by status" };  
        //yield return new object[] { "Find purchase order by id 3" };  
        //yield return new object[] { "Find pet by id 5" };  
        //yield return new object[] { "Returns pet inventories by status" };  //ERROR
        //yield return new object[] { "Find purchase order by id 6" };   //ERROR
        //yield return new object[] { "Find pet by id 8" };  //ERROR
        //yield return new object[] { "Returns pet inventories by status" };  //ERROR
        //yield return new object[] { "Find purchase order by id 9" };  //ERROR
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CurlTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/2\" -H \"accept: application/json\"" };
        yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/8\" -H \"accept: application/json\"" };
        yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/store/order/3\"" };
        yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/store/order/6\"" };
        yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/store/order/9\"" };
        yield return new object[] { "curl -X PUT \"https://petstore3.swagger.io/api/v3/pet\" -H \"Content-Type: application/json\" -d '{\n  \"id\": 1,\n  \"name\": \"doggie 1\"\n}'" }; //Error
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
