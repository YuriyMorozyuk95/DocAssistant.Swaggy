using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DocAssistant.Ai.Services
{
    public interface ISwaggerAiAssistantService
    {
        Task<FunctionResult> AskApi(string userInput);
        Task<FunctionResult> SummarizeForNonTechnical(string input, string curl, string response);
        Task<ChatMessageContent> GenerateCurl(string userInput);
    }

    public class SwaggerAiAssistantService : ISwaggerAiAssistantService
    {
        private readonly ICurlExecutor _curlExecutor;
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatService;

        //TODO add filePath to config
        public SwaggerAiAssistantService(IConfiguration configuration, OpenAIClient openAiClient, ICurlExecutor curlExecutor)
        {
            var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];

            _curlExecutor = curlExecutor;
            _kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deployedModelName, openAiClient)
                .AddAzureOpenAITextGeneration(deployedModelName, openAiClient)
                .Build();

            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        //TODO return response and curl as well, and calculate metadata
        public async Task<FunctionResult> AskApi(string userInput)
        {
            var curlChatMessage = await GenerateCurl(userInput);
            var curl = curlChatMessage.Content;

            var response = await _curlExecutor.ExecuteCurl(curl);
            var completion = await SummarizeForNonTechnical(userInput, curl, response);

            return completion;
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

        public async Task<ChatMessageContent> GenerateCurl(string userInput)
        {
            var systemPrompt = await GenerateSystemPrompt();

            var getQueryChat = new ChatHistory(systemPrompt);
            getQueryChat.AddUserMessage(userInput);

            var chatMessage = await _chatService.GetChatMessageContentsAsync(getQueryChat);

            return chatMessage[0];
        }

        private async Task<string> GenerateSystemPrompt(CancellationToken cancellationToken = default)
        {
            string swaggerFilePath = "Assets/petstore-swagger-full.json";
            string swaggerPromptFilePath = "Assets/system-prompt-swagger.txt";

            var swaggerFile = await File.ReadAllTextAsync(swaggerFilePath, cancellationToken);
            var swaggerPrompt = await File.ReadAllTextAsync(swaggerPromptFilePath, cancellationToken);

            var systemPrompt = swaggerPrompt.Replace("{{swagger-file}}", swaggerFile);
            return systemPrompt;
        }
    }
}
