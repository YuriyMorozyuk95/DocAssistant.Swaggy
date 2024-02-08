using DocAssistant.Ai.Services;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.DataFormats.Text;

namespace DocAssistant.Ai.MemoryHandlers
{
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
        StepName = stepName;
        _orchestrator = orchestrator;

        _options = options ?? new TextPartitioningOptions();
        _options.Validate();

        _log = log ?? DefaultLogger<SwaggerPartitioningHandler>.Instance;
        _log.LogInformation("Handler '{0}' ready", stepName);

        _tokenCounter = DefaultGPTTokenizer.StaticCountTokens;
        if (orchestrator.EmbeddingGenerationEnabled)
        {
            foreach (var gen in orchestrator.GetEmbeddingGenerators())
            {
                // Use the last tokenizer (TODO: revisit)
                _tokenCounter = s => gen.CountTokens(s);
                _maxTokensPerPartition = Math.Min(gen.MaxTokens, _maxTokensPerPartition);
            }
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        IndexCreationInformation.IndexCreationInfo.StepInfo = $"{StepName}: partitioning text in small chunks.";

        _log.LogDebug("Partitioning text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var file = generatedFile.Value;
                if (file.AlreadyProcessedBy(this))
                {
                    _log.LogTrace("File {0} already processed by this handler", file.Name);
                    continue;
                }

                // Partition only the original text
                if (file.ArtifactType != DataPipeline.ArtifactTypes.ExtractedText)
                {
                    _log.LogTrace("Skipping file {0} (not original text)", file.Name);
                    continue;
                }

                BinaryData partitionContent = await _orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);

                // Skip empty partitions. Also: partitionContent.ToString() throws an exception if there are no bytes.
                if (partitionContent.ToArray().Length == 0) { continue; }

                var partitions = CreatePartitions(file, partitionContent).ToList();

                if (partitions.Count == 0) { continue; }

                _log.LogDebug("Saving {0} file partitions", partitions.Count);
                for (int index = 0; index < partitions.Count; index++)
                {
                    await AddPartition(pipeline, cancellationToken, partitions, index, uploadedFile, newFiles);
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

    private IEnumerable<(string path, string partition)> CreatePartitions(DataPipeline.GeneratedFileDetails file, BinaryData partitionContent)
    {
        IEnumerable<(string, string)> partitions = new List<(string path, string partition)>();

        // Use a different partitioning strategy depending on the file type
        switch (file.MimeType)
        {
            case MimeTypes.PlainText:
            {
                _log.LogDebug("Partitioning text file {0}", file.Name);
                string content = partitionContent.ToString();

                        var stepProgress = new Progress<(int max, int value)>(value =>
                        {
                            IndexCreationInformation.IndexCreationInfo.Value = value.value;
                            IndexCreationInformation.IndexCreationInfo.Max = value.max;
                        });

                        partitions = SwaggerSplitter.SplitSwagger(content, stepProgress);
                break;
            }

            // TODO: add virtual/injectable logic
            // TODO: see https://learn.microsoft.com/en-us/windows/win32/search/-search-ifilter-about

            default:
                _log.LogWarning("File {0} cannot be partitioned, type '{1}' not supported", file.Name, file.MimeType);
                // Don't partition other files
                return partitions;
        }

        return partitions;
    }

    private async Task AddPartition(DataPipeline pipeline, CancellationToken cancellationToken, List<(string path, string document)> partitions, int index,
        DataPipeline.FileDetails uploadedFile, Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles)
    {
        string endpoint = partitions[index].path;
        string text = partitions[index].document;
        BinaryData textData = new(text);

        int tokenCount = _tokenCounter(text);
        _log.LogDebug("Partition size: {0} tokens", tokenCount);

        var destFile = uploadedFile.GetPartitionFileName(index);
        await _orchestrator.WriteFileAsync(pipeline, destFile, textData, cancellationToken).ConfigureAwait(false);

        var destFileDetails = new DataPipeline.GeneratedFileDetails
        {
            Id = Guid.NewGuid().ToString("N"),
            ParentId = uploadedFile.Id,
            Name = destFile,
            Size = text.Length,
            MimeType = MimeTypes.PlainText,
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            Tags = new TagCollection(),
            ContentSHA256 = textData.CalculateSHA256(),
        };

        pipeline.Tags.CopyTo(destFileDetails.Tags);
        destFileDetails.Tags.Add(TagsKeys.Endpoint, endpoint);

        newFiles.Add(destFile, destFileDetails);
        destFileDetails.MarkProcessedBy(this);
    }
}
}
