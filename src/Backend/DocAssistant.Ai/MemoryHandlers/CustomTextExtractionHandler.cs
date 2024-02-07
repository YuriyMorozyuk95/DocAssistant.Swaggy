using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.WebPages;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using System.Text;
using DocAssistant.Ai.Services;

namespace DocAssistant.Ai.MemoryHandlers
{
    /// <summary>
    /// Memory ingestion pipeline handler responsible for extracting text from files and saving it to content storage.
    /// </summary>
    public class CustomTextExtractionHandler : IPipelineStepHandler
    {
        private readonly IPipelineOrchestrator _orchestrator;
        private readonly IDocumentStorageService _documentStorageService;
        private readonly WebScraper _webScraper;
        private readonly IOcrEngine? _ocrEngine;
        private readonly ILogger<CustomTextExtractionHandler> _log;

        /// <inheritdoc />
        public string StepName { get; }

        /// <summary>
        /// Handler responsible for extracting text from documents.
        /// Note: stepName and other params are injected with DI.
        /// </summary>
        /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
        /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
        /// <param name="ocrEngine">The ocr engine to use for parsing image files</param>
        /// <param name="log">Application logger</param>
        public CustomTextExtractionHandler(
            string stepName,
            IPipelineOrchestrator orchestrator,
            IDocumentStorageService documentStorageService,
            IOcrEngine? ocrEngine = null,
            ILogger<CustomTextExtractionHandler>? log = null)
        {
            StepName = stepName;
            _orchestrator = orchestrator;
            _documentStorageService = documentStorageService;
            _ocrEngine = ocrEngine;
            _log = log ?? DefaultLogger<CustomTextExtractionHandler>.Instance;
            _webScraper = new WebScraper(_log);

            _log.LogInformation("Handler '{0}' ready", stepName);
        }

        /// <inheritdoc />
        public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
            DataPipeline pipeline, CancellationToken cancellationToken = default)
        {
            IndexCreationInformation.IndexCreationInfo.StepInfo = $"{StepName}: extracting text from files and saving it to content storage.";

            _log.LogDebug("Extracting text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

            foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
            {
                if (uploadedFile.AlreadyProcessedBy(this))
                {
                    _log.LogTrace("File {0} already processed by this handler", uploadedFile.Name);
                    continue;
                }

                await UpdateSourceFileMetadata(pipeline.DocumentId, uploadedFile.Name);

                var sourceFile = uploadedFile.Name;
                var destFile = $"{uploadedFile.Name}.extract.txt";
                BinaryData fileContent = await _orchestrator.ReadFileAsync(pipeline, sourceFile, cancellationToken).ConfigureAwait(false);

                string text = string.Empty;
                string extractType = MimeTypes.PlainText;
                bool skipFile = false;

                if (fileContent.ToArray().Length > 0)
                {
                    (text, extractType, skipFile) = await ExtractTextAsync(uploadedFile, fileContent, cancellationToken).ConfigureAwait(false);
                }

                // If the handler cannot extract text, we move on. There might be other handlers in the pipeline
                // capable of doing so, and in any case if a document contains multiple docs, the pipeline will
                // not fail, only do its best to export as much data as possible. The user can inspect the pipeline
                // status to know if a file has been ignored.
                if (!skipFile)
                {
                    _log.LogDebug("Saving extracted text file {0}", destFile);
                    await _orchestrator.WriteFileAsync(pipeline, destFile, new BinaryData(text), cancellationToken).ConfigureAwait(false);

                    var destFileDetails = new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        MimeType = extractType,
                        ArtifactType = DataPipeline.ArtifactTypes.ExtractedText,
                        Tags = pipeline.Tags,
                    };
                    destFileDetails.MarkProcessedBy(this);

                    uploadedFile.GeneratedFiles.Add(destFile, destFileDetails);
                }

                uploadedFile.MarkProcessedBy(this);
            }

            return (true, pipeline);
        }

        public async Task UpdateSourceFileMetadata(string uploadedFileId, string uploadedFileName)
        {
            await _documentStorageService.SetOriginFlagMetadata(uploadedFileId, uploadedFileName);
        }

        private async Task<(string text, string extractType, bool skipFile)> ExtractTextAsync(
            DataPipeline.FileDetails uploadedFile,
            BinaryData fileContent,
            CancellationToken cancellationToken)
        {
            bool skipFile = false;
            string text = string.Empty;
            string extractType = MimeTypes.PlainText;

            switch (uploadedFile.MimeType)
            {
                case MimeTypes.PlainText:
                    _log.LogDebug("Extracting text from plain text file {0}", uploadedFile.Name);
                    text = fileContent.ToString();
                    break;

                case MimeTypes.MarkDown:
                    _log.LogDebug("Extracting text from MarkDown file {0}", uploadedFile.Name);
                    text = fileContent.ToString();
                    extractType = MimeTypes.MarkDown;
                    break;

                case MimeTypes.Json:
                    _log.LogDebug("Extracting text from JSON file {0}", uploadedFile.Name);
                    text = fileContent.ToString();
                    break;

                case MimeTypes.MsWord:
                    _log.LogDebug("Extracting text from MS Word file {0}", uploadedFile.Name);
                    text = new MsWordDecoder().DocToText(fileContent);
                    break;

                case MimeTypes.MsPowerPoint:
                    _log.LogDebug("Extracting text from MS PowerPoint file {0}", uploadedFile.Name);
                    text = new MsPowerPointDecoder().DocToText(fileContent,
                        withSlideNumber: true,
                        withEndOfSlideMarker: false,
                        skipHiddenSlides: true);
                    break;

                case MimeTypes.MsExcel:
                    _log.LogDebug("Extracting text from MS Excel file {0}", uploadedFile.Name);
                    text = new MsExcelDecoder().DocToText(fileContent);
                    break;

                case MimeTypes.Pdf:
                    _log.LogDebug("Extracting text from PDF file {0}", uploadedFile.Name);

                    // TODO: carry over page numbers, e.g. using special tokens
                    var pages = new PdfDecoder().DocToText(fileContent);
                    var textBuilder = new StringBuilder();
                    foreach (var page in pages)
                    {
                        textBuilder.Append(page.Text.Trim());
                        textBuilder.AppendLine();
                        textBuilder.AppendLine();
                    }

                    text = textBuilder.ToString().Trim();

                    break;

                case MimeTypes.WebPageUrl:
                    var url = fileContent.ToString();
                    _log.LogDebug("Downloading web page specified in {0} and extracting text from {1}", uploadedFile.Name, url);
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        skipFile = true;
                        uploadedFile.Log(this, "The web page URL is empty");
                        _log.LogWarning("The web page URL is empty");
                        break;
                    }

                    var result = await _webScraper.GetTextAsync(url).ConfigureAwait(false);
                    if (!result.Success)
                    {
                        skipFile = true;
                        uploadedFile.Log(this, $"Download error: {result.Error}");
                        _log.LogWarning("Web page download error: {0}", result.Error);
                        break;
                    }

                    if (string.IsNullOrEmpty(result.Text))
                    {
                        skipFile = true;
                        uploadedFile.Log(this, "The web page has no text content, skipping it");
                        _log.LogWarning("The web page has no text content, skipping it");
                        break;
                    }

                    text = result.Text;
                    _log.LogDebug("Web page {0} downloaded, text length: {1}", url, text.Length);
                    break;

                case "":
                    skipFile = true;
                    uploadedFile.Log(this, "File MIME type is empty, ignoring the file");
                    _log.LogWarning("Empty MIME type, the file will be ignored");
                    break;

                case MimeTypes.ImageJpeg:
                case MimeTypes.ImagePng:
                case MimeTypes.ImageTiff:
                    _log.LogDebug("Extracting text from image file {0}", uploadedFile.Name);
                    if (_ocrEngine == null)
                    {
                        throw new NotSupportedException($"Image extraction not configured: {uploadedFile.Name}");
                    }

                    text = await new ImageDecoder().ImageToTextAsync(_ocrEngine, fileContent, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    skipFile = true;
                    uploadedFile.Log(this, $"File MIME type not supported: {uploadedFile.MimeType}. Ignoring the file.");
                    _log.LogWarning("File MIME type not supported: {0} - ignoring the file", uploadedFile.MimeType);
                    break;
            }

            return (text, extractType, skipFile);
        }
    }
}
