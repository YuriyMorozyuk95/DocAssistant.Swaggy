using System;
using System.Net;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using DocAssistant.Ai.MemoryHandlers;
using DocAssistant.Ai.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;
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

            kernel.ImportPluginFromPromptDirectory("Prompts");

            return kernel;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var azureOpenAiTextConfig = new AzureOpenAIConfig();
            var azureOpenAiEmbeddingConfig = new AzureOpenAIConfig();
            var searchClientConfig = new AzureAISearchConfig();
            var azureBlobConfig = new AzureBlobsConfig();
            var azureAiSearchConfig = new AzureAISearchConfig();
            config.BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAiTextConfig);
            config.BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAiEmbeddingConfig);
            config.BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);
            config.BindSection("KernelMemory:Services:AzureBlobs", azureBlobConfig);
            config.BindSection("KernelMemory:Services:AzureAISearch", azureAiSearchConfig);

            var services = new ServiceCollection(); 
            services.AddHandlerAsHostedService<TextExtractionHandler>(Constants.PipelineStepsExtract);
            services.AddHandlerAsHostedService<SwaggerPartitioningHandler>(Constants.PipelineStepsPartition);
            services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
            services.AddHandlerAsHostedService<SaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
            services.AddHandlerAsHostedService<SummarizationHandler>(Constants.PipelineStepsSummarize);
            services.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
            services.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
            services.AddHandlerAsHostedService<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);

            var memory = new KernelMemoryBuilder(services)
                .WithAzureOpenAITextGeneration(azureOpenAiTextConfig)
                .WithAzureOpenAITextEmbeddingGeneration(azureOpenAiEmbeddingConfig)
                .WithAzureBlobsStorage(azureBlobConfig)                             
                .WithAzureAISearchMemoryDb(azureAiSearchConfig)
                .WithoutDefaultHandlers()
                .Build<MemoryServerless>();

            var serviceProvider = services.BuildServiceProvider();

            memory.AddHandler(serviceProvider.GetRequiredService<TextExtractionHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<SwaggerPartitioningHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<GenerateEmbeddingsHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<SaveRecordsHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<SummarizationHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<DeleteDocumentHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<DeleteIndexHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<DeleteGeneratedFilesHandler>());

            return memory;
        });

        services.AddTransient<ICurlExecutor, CurlExecutor>();
        services.AddTransient<ISwaggerMemorySearchService, SwaggerMemorySearchService>();
        services.AddTransient<ISwaggerAiAssistantService, SwaggerAiAssistantService>();
    }
}
