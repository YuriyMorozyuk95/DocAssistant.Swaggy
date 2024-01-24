﻿using System.Net;

using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Search.Documents.Indexes;
using GunvorCopilot.Data;
using GunvorCopilot.Data.Interfaces;
using Microsoft.Azure.Cosmos;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static void AddAzureServices(this IServiceCollection services)
    {
        services.AddSingleton<DefaultAzureCredential>();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccountConnectionString = config["AzureStorageAccountEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountConnectionString);

            var credential = new DefaultAzureCredential();

            var blobServiceClient = new BlobServiceClient(new Uri(azureStorageAccountConnectionString), credential);

            return blobServiceClient;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["InputAzureStorageContainer"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        services.AddSingleton<IStorageService, StorageService>();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchServiceEndpoint, azureSearchIndex, key) =
                (config["AzureSearchServiceEndpoint"], config["AzureSearchIndex"], config["AzureSearchServiceEndpointKey"]);

            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);

            var credential = new AzureKeyCredential(key!);

            var searchClient = new SearchClient(
                new Uri(azureSearchServiceEndpoint), azureSearchIndex, credential, new SearchClientOptions
                {
                    Transport = new HttpClientTransport(new HttpClient(new HttpClientHandler()
                    {
                        Proxy = new WebProxy()
                        {
                            BypassProxyOnLocal = false,
                            UseDefaultCredentials = true,
                        }
                    }))
                });


            return searchClient;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchServiceEndpoint, key) =
                (config["AzureSearchServiceEndpoint"], config["AzureSearchServiceEndpointKey"]);

            var credential = new AzureKeyCredential(key!);

            var searchIndexClient = new SearchIndexClient(
                new Uri(azureSearchServiceEndpoint!),
                credential, new SearchClientOptions
                {
                    Transport = new HttpClientTransport(new HttpClient(new HttpClientHandler()
                    {
                        Proxy = new WebProxy()
                        {
                            BypassProxyOnLocal = false,
                            UseDefaultCredentials = true,
                        }
                    }))
                });

            return searchIndexClient;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config["AzureDocumentIntelligenceEndpoint"] ?? throw new ArgumentNullException();
            var key = config["AzureDocumentIntelligenceEndpointKey"] ?? throw new ArgumentNullException();

            var credential = new AzureKeyCredential(key!);

            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(azureOpenAiServiceEndpoint), credential, new DocumentAnalysisClientOptions
                {
                    Transport = new HttpClientTransport(new HttpClient(new HttpClientHandler()
                    {
                        Proxy = new WebProxy()
                        {
                            BypassProxyOnLocal = false,
                            UseDefaultCredentials = true,
                        }
                    }))
                });
            return documentAnalysisClient;
        });

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

        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<ReadRetrieveReadChatService>();
        services.AddSingleton<IUploaderDocumentService, UploaderDocumentService>();
        services.AddSingleton<IAzureSearchEmbedService, AzureSearchAzureSearchEmbedService>();
    }

    internal static void AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));
    }

    internal static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<CosmosClient>(x =>
        {
            var endpoint = configuration["CosmosDB:EndpointUrl"];
            var key = configuration["CosmosDB:Key"];

            var credential = new AzureKeyCredential(key!);
            var cosmosClient = new CosmosClient(endpoint, credential);

            return cosmosClient;
        });

        services.AddSingleton<IUserRepository, UserRepository>();
        return services;
    }

    public static async Task CheckCosmosClientSetup(this WebApplication app)
    {
        try
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();
            var cosmosClient = app.Services.GetRequiredService<CosmosClient>();
            var logger = app.Services.GetRequiredService<ILogger<CosmosClient>>();

            var cosmosDbName = configuration["CosmosDB:Name"];
            await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbName);

            // If the operation is successful, the setup is correct
            logger.LogInformation("CosmosClient is set up correctly.");
        }
        catch (CosmosException ex)
        {
            // If the operation fails, throw an exception
            throw new Exception("CosmosClient is not set up correctly.", ex);
        }
    }
}
