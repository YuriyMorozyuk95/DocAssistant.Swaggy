using Azure.AI.OpenAI;
using Shared.Models.Swagger;

namespace DocAssistant.Ai.Services
{
    public interface IHttpRequestExecutor
    {
        Task<(ApiResponse, CompletionsUsage?)> Execute(SwaggerDocument document, string userInput);
    }
}
