using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models.Swagger;

namespace DocAssistant.Ai.Services;

public class HttpClientHttpRequestExecutor : IHttpRequestExecutor
{
    private readonly string _swaggerPrompt;
    private readonly IChatCompletionService _chatService;

    public HttpClientHttpRequestExecutor(IConfiguration configuration, Kernel kernel)
    {
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _swaggerPrompt = ReadPrompt(configuration);
    }

    public async Task<(ApiResponse, CompletionsUsage?)> Execute(SwaggerDocument document, string userInput)
    {
        var chatMessage = await GenerateRequestString(document.SwaggerContent, userInput);

        var requestString = chatMessage.Content;
        var metadata = chatMessage.Metadata["Usage"] as CompletionsUsage;


        var requestParts = requestString.Split(' ', 3);

        string? verb, url, body = null;
        if (requestParts.Length == 2)
        {
            verb = requestParts[0];
            url = requestParts[1];
        }
        else if (requestParts.Length == 3)
        {
            verb = requestParts[0];
            url = requestParts[1];
            body = requestParts[2];
        }
        else
        {
            throw new Exception("Could not create HTTP request");
        }

        // TODO: use static client
        // TODO: add api token
        var client = new HttpClient();

        HttpResponseMessage response;
        if (verb == "GET")
        {
            response = await client.GetAsync(url);
        }
        else if (verb == "POST" && body != null)
        {
            response = await client.PostAsync(url, new StringContent(body));
        }
        else if (verb == "PUT" && body != null)
        {
            response = await client.PutAsync(url, new StringContent(body));
        }
        else if (verb == "PATCH" && body != null)
        {
            response = await client.PatchAsync(url, new StringContent(body));
        }
        else if (verb == "DELETE")
        {
            response = await client.DeleteAsync(url);
        }
        else
        {
            throw new Exception("HTTP method not supported");
        }

        var apiResponse = new ApiResponse
        {
            Request = requestString,
            Code = (int)response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode,
            Message = await response.Content.ReadAsStringAsync()
        };
        apiResponse.Result = apiResponse.Message;

        return (apiResponse, metadata);
    }

    private string ReadPrompt(IConfiguration configuration)
    {
        var swaggerPromptFilePath = configuration["SwaggerAiAssistant:HttpClientSystemPromptSwaggerPath"];
        var path = string.Concat(AppContext.BaseDirectory, swaggerPromptFilePath);
        return File.ReadAllText(path);
    }

    private async Task<ChatMessageContent> GenerateRequestString(string swaggerFile, string userInput)
    {
        var systemPrompt = GenerateSystemPrompt(swaggerFile);

        var getQueryChat = new ChatHistory(systemPrompt);
        getQueryChat.AddUserMessage(userInput);

        var chatMessage = await _chatService.GetChatMessageContentsAsync(getQueryChat);

        return chatMessage[0];
    }

    private string GenerateSystemPrompt(string swaggerFile)
    {
        var systemPrompt = _swaggerPrompt.Replace("{{swagger-file}}", swaggerFile);
        return systemPrompt;
    }

}