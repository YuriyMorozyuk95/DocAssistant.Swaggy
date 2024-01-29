using System.Net;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using DocAssistant.Ai.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace DocAssistant.Ai;

public static class AiServiceCollectionExtensions
{
    public static void AddAiServices(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureOpenAiServiceEndpoint, key) = (config["AzureOpenAiServiceEndpoint"], config["AzureOpenAiServiceEndpointKey"]);

            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

            var credential = new AzureKeyCredential(key!);

            var openAiClient = new OpenAIClient(
                new Uri(azureOpenAiServiceEndpoint), credential, new OpenAIClientOptions
                {
                    Diagnostics = { IsLoggingContentEnabled = true },
                    Transport = new HttpClientTransport(new HttpClient(new HttpClientHandler()
                    {
                        Proxy = new WebProxy()
                        {
                            BypassProxyOnLocal = false,
                            UseDefaultCredentials = true,
                        }
                    }))
                });

            return openAiClient;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var openAiClient = sp.GetRequiredService<OpenAIClient>();
            var deployedModelName = config["AzureOpenAiChatGptDeployment"];

            var kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deployedModelName, openAiClient)
                .AddAzureOpenAITextGeneration(deployedModelName, openAiClient)
                .Build();

            return kernel;
        });

        services.AddTransient<ICurlExecutor, CurlExecutor>();
        services.AddTransient<ISwaggerAiAssistantService, SwaggerAiAssistantService>();
    }
}
