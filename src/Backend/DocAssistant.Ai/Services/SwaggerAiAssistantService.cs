using Azure.AI.OpenAI;
using DocAssistant.Ai.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DocAssistant.Ai.Services
{
    public interface ISwaggerAiAssistantService
    {
        Task<SwaggerCompletionInfo> AskApi(string swaggerFile, string userInput);

        Task<FunctionResult> SummarizeForNonTechnical(string input, string curl, string response);
        Task<ChatMessageContent> GenerateCurl(string swaggerFile, string userInput);
    }

    public class SwaggerAiAssistantService : ISwaggerAiAssistantService
    {
        private readonly ICurlExecutor _curlExecutor;
        private readonly IChatCompletionService _chatService;
        private readonly string _swaggerPrompt;
        private readonly Kernel _kernel;

        public SwaggerAiAssistantService(IConfiguration configuration, Kernel kernel, ICurlExecutor curlExecutor)
        {
            var swaggerPromptFilePath = configuration["SwaggerAiAssistant:SystemPromptSwaggerPath"];

            _curlExecutor = curlExecutor;
            _kernel = kernel;
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
            
            var path = string.Concat(AppContext.BaseDirectory, swaggerPromptFilePath);

            _swaggerPrompt = File.ReadAllText(path);
        }

        public async Task<SwaggerCompletionInfo> AskApi(string swaggerFile, string userInput)
        {
            var curlChatMessage = await GenerateCurl(swaggerFile, userInput);
            var curl = curlChatMessage.Content;

            var curlMetadata = curlChatMessage.Metadata["Usage"] as CompletionsUsage;

            var apiResponse = await _curlExecutor.ExecuteCurl(curl);
            var response = apiResponse.Result;

            var completion = await SummarizeForNonTechnical(userInput, curl, response);

            var summaryMetadata = completion.Metadata["Usage"] as CompletionsUsage;

            var swaggerCompletionInfo = new SwaggerCompletionInfo
            {
                FinalleResult = completion?.ToString(),
                Curl = curl,
                Response = apiResponse,
                CompletionTokens = curlMetadata.CompletionTokens + summaryMetadata.CompletionTokens,
                PromptTokens = curlMetadata.PromptTokens + summaryMetadata.PromptTokens,
                TotalTokens = curlMetadata.TotalTokens + summaryMetadata.TotalTokens,
            };

            return swaggerCompletionInfo;
        }

        public async Task<FunctionResult> SummarizeForNonTechnical(string input, string curl, string response)
        {
            var prompts = _kernel.ImportPluginFromPromptDirectory("Prompts");

            var summaryPrompt = prompts["SummarizeForNonTechnical"];

            var chatResult = await _kernel.InvokeAsync(
                summaryPrompt,
                new KernelArguments() {
                    { "input", input },
                    { "curl", curl },
                    { "response", response }
                }
            );

            return chatResult;
        }

        public async Task<ChatMessageContent> GenerateCurl(string swaggerFile, string userInput)
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
}
