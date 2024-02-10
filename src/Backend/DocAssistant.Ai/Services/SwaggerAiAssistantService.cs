using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Shared.Models.Swagger;

namespace DocAssistant.Ai.Services
{
    public interface ISwaggerAiAssistantService
    {
        Task<SwaggerCompletionInfo> AskApi(SwaggerDocument swaggerFile, string userInput);
        Task<SwaggerCompletionInfo> AskApi(string userInput);

        Task<FunctionResult> SummarizeForNonTechnical(string input, string endpoint, string response);
    }

    public class SwaggerAiAssistantService : ISwaggerAiAssistantService
    {
        private readonly ISwaggerMemorySearchService _swaggerMemorySearchService;
        private readonly IHttpRequestExecutor _httpRequestExecutor;
        private readonly ILogger<SwaggerAiAssistantService> _logger;
        private readonly Kernel _kernel;

        public SwaggerAiAssistantService(Kernel kernel,
            ISwaggerMemorySearchService swaggerMemorySearchService,
            IHttpRequestExecutor httpRequestExecutor,
            ILogger<SwaggerAiAssistantService> logger)
        {
            _swaggerMemorySearchService = swaggerMemorySearchService;
            _httpRequestExecutor = httpRequestExecutor;
            _logger = logger;
            _kernel = kernel;
            _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<SwaggerCompletionInfo> AskApi(string userInput)
        {
            try
            {
                var swaggerDocument = await _swaggerMemorySearchService.SearchDocument(userInput);

                var result = await AskApi(swaggerDocument, userInput);
                result.SwaggerDocument = swaggerDocument;

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error happen while generating answer");
                throw;
            }
        }

        public async Task<SwaggerCompletionInfo> AskApi(SwaggerDocument swaggerFile, string userInput)
        {
            var (response, usage) = await _httpRequestExecutor.Execute(swaggerFile, userInput);

            var completion = await SummarizeForNonTechnical(userInput, string.Empty, response.Result);

            var summaryMetadata = completion.Metadata["Usage"] as CompletionsUsage;

            var swaggerCompletionInfo = new SwaggerCompletionInfo
            {
                FinalResult = completion?.ToString(),
                Endpoint = response.Request,
                Response = response,
                CompletionTokens = usage.CompletionTokens + summaryMetadata.CompletionTokens,
                PromptTokens = usage.PromptTokens + summaryMetadata.PromptTokens,
                TotalTokens = usage.TotalTokens + summaryMetadata.TotalTokens,
            };

            return swaggerCompletionInfo;
        }

        public async Task<FunctionResult> SummarizeForNonTechnical(string input, string endpoint, string response)
        {
            var prompts = _kernel.Plugins["Prompts"]["SummarizeForNonTechnical"];

            var chatResult = await _kernel.InvokeAsync(
                prompts,
                new KernelArguments() {
                    { "input", input },
                    { "endpoint", endpoint },
                    { "response", response }
                }
            );

            return chatResult;
        }
    }
}
