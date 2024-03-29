﻿using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory;
using System.Text.Json;
using System.Security.Cryptography;

namespace DocAssistant.Ai.MemoryHandlers
{
    /// <summary>
    /// Memory ingestion pipeline handler responsible for generating text embedding and saving them to the content storage.
    /// </summary>
    public class CustomGenerateEmbeddingsHandler : IPipelineStepHandler
    {
        private readonly IPipelineOrchestrator _orchestrator;
        private readonly ILogger<CustomGenerateEmbeddingsHandler> _log;
        private readonly List<ITextEmbeddingGenerator> _embeddingGenerators;
        private readonly bool _embeddingGenerationEnabled;

        /// <inheritdoc />
        public string StepName { get; }

        /// <summary>
        /// Handler responsible for generating embeddings and saving them to content storages (not memory db).
        /// Note: stepName and other params are injected with DI
        /// </summary>
        /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
        /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
        /// <param name="log">Application logger</param>
        public CustomGenerateEmbeddingsHandler(
            string stepName,
            IPipelineOrchestrator orchestrator,
            ILogger<CustomGenerateEmbeddingsHandler>? log = null)
        {
            this.StepName = stepName;
            this._log = log ?? DefaultLogger<CustomGenerateEmbeddingsHandler>.Instance;
            this._embeddingGenerationEnabled = orchestrator.EmbeddingGenerationEnabled;

            this._orchestrator = orchestrator;
            this._embeddingGenerators = orchestrator.GetEmbeddingGenerators();

            if (this._embeddingGenerationEnabled)
            {
                if (this._embeddingGenerators.Count < 1)
                {
                    this._log.LogError("Handler '{0}' NOT ready, no embedding generators configured", stepName);
                }

                this._log.LogInformation("Handler '{0}' ready, {1} embedding generators", stepName, this._embeddingGenerators.Count);
            }
            else
            {
                this._log.LogInformation("Handler '{0}' ready, embedding generation DISABLED", stepName);
            }
        }

        /// <inheritdoc />
        public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
            DataPipeline pipeline, CancellationToken cancellationToken = default)
        {
            IndexCreationInformation.IndexCreationInfo.StepInfo = $"{StepName}:  Memory ingestion for generating text embedding and saving them to the content storage.";

            if (!this._embeddingGenerationEnabled)
            {
                this._log.LogTrace("Embedding generation is disabled, skipping - pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);
                return (true, pipeline);
            }

            this._log.LogDebug("Generating embeddings, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

            foreach (var uploadedFile in pipeline.Files)
            {
                // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
                Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

                IndexCreationInformation.IndexCreationInfo.Value = 0;
                IndexCreationInformation.IndexCreationInfo.Max = uploadedFile.GeneratedFiles.Count();
                foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
                {
                    IndexCreationInformation.IndexCreationInfo.Value++;
                    var partitionFile = generatedFile.Value;
                    if (partitionFile.AlreadyProcessedBy(this))
                    {
                        this._log.LogTrace("File {0} already processed by this handler", partitionFile.Name);
                        continue;
                    }

                    // Calc embeddings only for partitions (text chunks) and synthetic data
                    if (partitionFile.ArtifactType != DataPipeline.ArtifactTypes.TextPartition
                        && partitionFile.ArtifactType != DataPipeline.ArtifactTypes.SyntheticData)
                    {
                        this._log.LogTrace("Skipping file {0} (not a partition, not synthetic data)", partitionFile.Name);
                        continue;
                    }

                    // TODO: cost/perf: if the partition SHA256 is the same and the embedding exists, avoid generating it again
                    switch (partitionFile.MimeType)
                    {
                        case MimeTypes.PlainText:
                        case MimeTypes.MarkDown:
                            this._log.LogTrace("Processing file {0}", partitionFile.Name);
                            foreach (ITextEmbeddingGenerator generator in this._embeddingGenerators)
                            {
                                try
                                {
                                    EmbeddingFileContent embeddingData = new()
                                    {
                                        SourceFileName = partitionFile.Name
                                    };

                                    var generatorProviderClassName = generator.GetType().FullName ?? generator.GetType().Name;
                                    embeddingData.GeneratorProvider = string.Join('.', generatorProviderClassName.Split('.').TakeLast(3));

                                    // TODO: model name
                                    embeddingData.GeneratorName = "TODO";

                                    this._log.LogTrace("Generating embeddings using {0}, file: {1}", embeddingData.GeneratorProvider, partitionFile.Name);

                                    // Check if embeddings have already been generated
                                    string embeddingFileName = GetEmbeddingFileName(partitionFile.Name, embeddingData.GeneratorProvider, embeddingData.GeneratorName);

                                    // TODO: check if the file exists in storage
                                    if (uploadedFile.GeneratedFiles.ContainsKey(embeddingFileName))
                                    {
                                        this._log.LogDebug("Embeddings for {0} have already been generated", partitionFile.Name);
                                        continue;
                                    }

                                    // TODO: handle Azure.RequestFailedException - BlobNotFound
                                    string partitionContent = await this._orchestrator.ReadTextFileAsync(pipeline, partitionFile.Name, cancellationToken).ConfigureAwait(false);

                                    var inputTokenCount = generator.CountTokens(partitionContent);
                                    if (inputTokenCount > generator.MaxTokens)
                                    {
                                        this._log.LogWarning("The content size ({0} tokens) exceeds the embedding generator capacity ({1} max tokens)", inputTokenCount, generator.MaxTokens);
                                    }

                                    Embedding embedding = await generator.GenerateEmbeddingAsync(partitionContent, cancellationToken).ConfigureAwait(false);
                                    embeddingData.Vector = embedding;
                                    embeddingData.VectorSize = embeddingData.Vector.Length;
                                    embeddingData.TimeStamp = DateTimeOffset.UtcNow;

                                    await Task.Delay(TimeSpan.FromSeconds(30));

                                    this._log.LogDebug("Saving embedding file {0}", embeddingFileName);
                                    string text = JsonSerializer.Serialize(embeddingData);
                                    await this._orchestrator.WriteTextFileAsync(pipeline, embeddingFileName, text, cancellationToken).ConfigureAwait(false);

                                    var embeddingFileNameDetails = new DataPipeline.GeneratedFileDetails
                                    {
                                        Id = Guid.NewGuid().ToString("N"),
                                        ParentId = uploadedFile.Id,
                                        SourcePartitionId = partitionFile.Id,
                                        Name = embeddingFileName,
                                        Size = text.Length,
                                        MimeType = MimeTypes.TextEmbeddingVector,
                                        ArtifactType = DataPipeline.ArtifactTypes.TextEmbeddingVector,
                                        Tags = partitionFile.Tags,
                                    };
                                    embeddingFileNameDetails.MarkProcessedBy(this);
                                    newFiles.Add(embeddingFileName, embeddingFileNameDetails);
                                }
                                catch (Exception e)
                                {
                                    await Task.Delay(TimeSpan.FromMinutes(2));
                                    continue;
                                }
                            }

                            break;

                        default:
                            this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", partitionFile.Name);
                            continue;
                    }

                    partitionFile.MarkProcessedBy(this);
                }

                // Add new files to pipeline status
                foreach (var file in newFiles)
                {
                    uploadedFile.GeneratedFiles.Add(file.Key, file.Value);
                }
            }

            return (true, pipeline);
        }

        private static string GetEmbeddingFileName(string srcFilename, string type, string embeddingName)
        {
            return $"{srcFilename}.{type}.{embeddingName}{FileExtensions.TextEmbeddingVector}";
        }
    }
}
