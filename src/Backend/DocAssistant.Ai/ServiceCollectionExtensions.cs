using System;
using System.Net;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using DocAssistant.Ai.MemoryHandlers;
using DocAssistant.Ai.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

            var path = string.Concat(AppContext.BaseDirectory, "Prompts");
            kernel.ImportPluginFromPromptDirectory(path);

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

            var kernelMemoryServiceCollection = new ServiceCollection { services };

            kernelMemoryServiceCollection.AddHandlerAsHostedService<CustomTextExtractionHandler>(Constants.PipelineStepsExtract);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<SwaggerPartitioningHandler>(Constants.PipelineStepsPartition);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<CustomGenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<CustomSaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<SummarizationHandler>(Constants.PipelineStepsSummarize);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
            kernelMemoryServiceCollection.AddHandlerAsHostedService<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);

            var memory = new KernelMemoryBuilder(kernelMemoryServiceCollection)
                .WithAzureOpenAITextGeneration(azureOpenAiTextConfig)
                .WithAzureOpenAITextEmbeddingGeneration(azureOpenAiEmbeddingConfig)
                .WithAzureBlobsStorage(azureBlobConfig)                             
                .WithAzureAISearchMemoryDb(azureAiSearchConfig)
                .WithoutDefaultHandlers()
                .Build<MemoryServerless>();

            var serviceProvider = kernelMemoryServiceCollection.BuildServiceProvider();

            memory.AddHandler(serviceProvider.GetRequiredService<CustomTextExtractionHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<SwaggerPartitioningHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<CustomGenerateEmbeddingsHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<CustomSaveRecordsHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<SummarizationHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<DeleteDocumentHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<DeleteIndexHandler>());
            memory.AddHandler(serviceProvider.GetRequiredService<DeleteGeneratedFilesHandler>());

            return memory;
        });

        services.AddTransient<ICurlExecutor, CurlExecutor>();

        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");  

        if (environment == "Development")
        {
            services.AddTransient<IHttpRequestExecutor, CurlHttpRequestExecutor>();
        }
        else
        {
            services.AddTransient<IHttpRequestExecutor, HttpClientHttpRequestExecutor>();
        }

        services.AddTransient<ISwaggerMemorySearchService, SwaggerMemorySearchService>();
        services.AddTransient<ISwaggerAiAssistantService, SwaggerAiAssistantService>();
        services.AddTransient<ISwaggerMemoryManagerService, SwaggerMemoryManagerService>();
        services.AddTransient<IDocumentStorageService, DocumentStorageService>();
    }
}
