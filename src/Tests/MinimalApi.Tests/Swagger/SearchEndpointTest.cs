using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.Pipeline;

namespace MinimalApi.Tests.Swagger
{
    public class SearchEndpointTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly MemoryServerless _memory;

        public SearchEndpointTest(WebApplicationFactory<Program> factory)
        {
            var config = factory.Services.GetRequiredService<IConfiguration>();

#pragma warning disable SKEXP0001
#pragma warning restore SKEXP0001

            var azureOpenAITextConfig = new AzureOpenAIConfig();
            var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
            var searchClientConfig = new AzureAISearchConfig();

            config.BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig);
            config.BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);
            config.BindSection("KernelMemory:Retrieval:SearchClient", searchClientConfig);

            var services = new ServiceCollection(); 
            services.AddHandlerAsHostedService<TextExtractionHandler>(Constants.PipelineStepsExtract);
            services.AddHandlerAsHostedService<SwaggerPartitioningHandler>(Constants.PipelineStepsPartition);
            services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
            services.AddHandlerAsHostedService<SaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
            services.AddHandlerAsHostedService<SummarizationHandler>(Constants.PipelineStepsSummarize);
            services.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
            services.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
            services.AddHandlerAsHostedService<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);

            _memory = new KernelMemoryBuilder(services)
                .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
                .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
                //.WithAzureBlobsStorage(new AzureBlobsConfig {...})                             // => use Azure Blobs
                //.WithAzureAISearchMemoryDb(new AzureAISearchConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureAISearchConfig.AuthTypes.APIKey })
                .WithoutDefaultHandlers()
                .Build<MemoryServerless>();

            var serviceProvider = services.BuildServiceProvider();

            _memory.AddHandler(serviceProvider.GetRequiredService<TextExtractionHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<SwaggerPartitioningHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<GenerateEmbeddingsHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<SaveRecordsHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<SummarizationHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<DeleteDocumentHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<DeleteIndexHandler>());
            _memory.AddHandler(serviceProvider.GetRequiredService<DeleteGeneratedFilesHandler>());
        }

        public async Task InitializeAsync()
        {
            await _memory.ImportDocumentAsync(new Document()
                .AddFile("Assets/petstore-swagger-create-user.json")
                .AddFile("Assets/petstore-swagger-order-create.json")
                .AddFile("Assets/petstore-swagger-order-find-by-id.json")
                .AddFile("Assets/petstore-swagger-order-inventories.json")
                .AddFile("Assets/petstore-swagger-create-user.json"));

        }

        [Fact]
        public async Task CanSearch()
        {
            var question = "Could you make an order for a pet with id 198773 with quantity 10?";
            var a = await _memory.SearchAsync(question);
            var b = await _memory.AskAsync(question);
        }

        

        public Task DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class SwaggerPartitioningHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly TextPartitioningOptions _options;
    private readonly ILogger<SwaggerPartitioningHandler> _log;
    private readonly TextChunker.TokenCounter _tokenCounter;
    private readonly int _maxTokensPerPartition = int.MaxValue;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for partitioning text in small chunks.
    /// Note: stepName and other params are injected with DI.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="options">The customize text partitioning option</param>
    /// <param name="log">Application logger</param>
    public SwaggerPartitioningHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        TextPartitioningOptions? options = null,
        ILogger<SwaggerPartitioningHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;

        this._options = options ?? new TextPartitioningOptions();
        this._options.Validate();

        this._log = log ?? DefaultLogger<SwaggerPartitioningHandler>.Instance;
        this._log.LogInformation("Handler '{0}' ready", stepName);

        this._tokenCounter = DefaultGPTTokenizer.StaticCountTokens;
        if (orchestrator.EmbeddingGenerationEnabled)
        {
            foreach (var gen in orchestrator.GetEmbeddingGenerators())
            {
                // Use the last tokenizer (TODO: revisit)
                this._tokenCounter = s => gen.CountTokens(s);
                this._maxTokensPerPartition = Math.Min(gen.MaxTokens, this._maxTokensPerPartition);
            }

            if (this._options.MaxTokensPerParagraph > this._maxTokensPerPartition)
            {
#pragma warning disable CA2254 // the msg is always used
                var errMsg = $"The configured partition size ({this._options.MaxTokensPerParagraph} tokens) is too big for one " +
                             $"of the embedding generators in use. The max value allowed is {this._maxTokensPerPartition} tokens. ";
                this._log.LogError(errMsg);
                throw new ConfigurationException(errMsg);
#pragma warning restore CA2254
            }
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Partitioning text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var file = generatedFile.Value;
                if (file.AlreadyProcessedBy(this))
                {
                    this._log.LogTrace("File {0} already processed by this handler", file.Name);
                    continue;
                }

                // Partition only the original text
                if (file.ArtifactType != DataPipeline.ArtifactTypes.ExtractedText)
                {
                    this._log.LogTrace("Skipping file {0} (not original text)", file.Name);
                    continue;
                }

                // Use a different partitioning strategy depending on the file type
                List<string> paragraphs;
                List<string> lines;
                BinaryData partitionContent = await this._orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);

                // Skip empty partitions. Also: partitionContent.ToString() throws an exception if there are no bytes.
                if (partitionContent.ToArray().Length == 0) { continue; }

                switch (file.MimeType)
                {
                    case MimeTypes.PlainText:
                    {
                        this._log.LogDebug("Partitioning text file {0}", file.Name);
                        string content = partitionContent.ToString();
                        //lines = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: this._options.MaxTokensPerLine, tokenCounter: this._tokenCounter);
                        //paragraphs = TextChunker.SplitPlainTextParagraphs(
                        //    lines, maxTokensPerParagraph: this._options.MaxTokensPerParagraph, overlapTokens: this._options.OverlappingTokens, tokenCounter: this._tokenCounter);
                        paragraphs = new List<string> () { content };
                        break;
                    }

                    case MimeTypes.MarkDown:
                    {
                        this._log.LogDebug("Partitioning MarkDown file {0}", file.Name);
                        string content = partitionContent.ToString();
                        lines = TextChunker.SplitMarkDownLines(content, maxTokensPerLine: this._options.MaxTokensPerLine, tokenCounter: this._tokenCounter);
                        paragraphs = TextChunker.SplitMarkdownParagraphs(
                            lines, maxTokensPerParagraph: this._options.MaxTokensPerParagraph, overlapTokens: this._options.OverlappingTokens, tokenCounter: this._tokenCounter);
                        break;
                    }

                    // TODO: add virtual/injectable logic
                    // TODO: see https://learn.microsoft.com/en-us/windows/win32/search/-search-ifilter-about

                    default:
                        this._log.LogWarning("File {0} cannot be partitioned, type '{1}' not supported", file.Name, file.MimeType);
                        // Don't partition other files
                        continue;
                }

                if (paragraphs.Count == 0) { continue; }

                this._log.LogDebug("Saving {0} file partitions", paragraphs.Count);
                for (int index = 0; index < paragraphs.Count; index++)
                {
                    string text = paragraphs[index];
                    BinaryData textData = new(text);

                    int tokenCount = this._tokenCounter(text);
                    this._log.LogDebug("Partition size: {0} tokens", tokenCount);

                    var destFile = uploadedFile.GetPartitionFileName(index);
                    await this._orchestrator.WriteFileAsync(pipeline, destFile, textData, cancellationToken).ConfigureAwait(false);

                    var destFileDetails = new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        MimeType = MimeTypes.PlainText,
                        ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
                        Tags = pipeline.Tags,
                        ContentSHA256 = textData.CalculateSHA256(),
                    };
                    newFiles.Add(destFile, destFileDetails);
                    destFileDetails.MarkProcessedBy(this);
                }

                file.MarkProcessedBy(this);
            }

            // Add new files to pipeline status
            foreach (var file in newFiles)
            {
                uploadedFile.GeneratedFiles.Add(file.Key, file.Value);
            }
        }

        return (true, pipeline);
    }
}
}
