using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models.Swagger;

namespace DocAssistant.Ai.Services;

public class CurlHttpRequestExecutor : IHttpRequestExecutor
{
    private readonly ICurlExecutor _curlExecutor;
    private readonly string _swaggerPrompt;
    private readonly IChatCompletionService _chatService;

    public CurlHttpRequestExecutor(IConfiguration configuration, Kernel kernel, ICurlExecutor curlExecutor)
    {
        _curlExecutor = curlExecutor;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _swaggerPrompt = ReadCurlPrompt(configuration);
    }

    public async Task<(ApiResponse, CompletionsUsage?)> Execute(SwaggerDocument document, string userInput)
    {
        var curlChatMessage = await GenerateCurl(document.SwaggerContent, userInput, document.ApiToken);

        var curl = curlChatMessage.Content;
        var curlMetadata = curlChatMessage.Metadata["Usage"] as CompletionsUsage;

        var apiResponse = await _curlExecutor.ExecuteCurl(curl);

        return (apiResponse, curlMetadata);
    }

    private string ReadCurlPrompt(IConfiguration configuration)
    {
        var swaggerPromptFilePath = configuration["SwaggerAiAssistant:CurlSystemPromptSwaggerPath"];
        var path = string.Concat(AppContext.BaseDirectory, swaggerPromptFilePath);
        return File.ReadAllText(path);
    }

    public async Task<ChatMessageContent> GenerateCurl(string swaggerFile, string userInput, string? apiKey = null)
    {
        var systemPrompt = GenerateSystemPrompt(swaggerFile, apiKey);

        var getQueryChat = new ChatHistory(systemPrompt);
        getQueryChat.AddUserMessage(userInput);

        var chatMessage = await _chatService.GetChatMessageContentsAsync(getQueryChat);

        return chatMessage[0];
    }

    private string GenerateSystemPrompt(string swaggerFile, string? apiKey)
    {
        var systemPrompt = _swaggerPrompt.Replace("{{swagger-file}}", swaggerFile).Replace("{{apiKey}}", apiKey);
        return systemPrompt;
    }

}