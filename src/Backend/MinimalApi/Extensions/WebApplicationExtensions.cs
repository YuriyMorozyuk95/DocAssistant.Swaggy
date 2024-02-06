using DocAssistant.Ai.Services;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        // Long-form chat w/ contextual history endpoint
        api.MapPost("chat", OnPostChatAsync).DisableAntiforgery();

        // Upload a document
        api.MapPost("documents", OnPostDocumentAsync).DisableAntiforgery();

        // Get all documents
        api.MapGet("documents", OnGetDocumentsAsync);

        api.MapDelete("clear", OnPostClearAsync).DisableAntiforgery();

        // Get synchronize status  
        api.MapGet("synchronize-status", OnGetIndexCreationInfo).DisableAntiforgery();

        return app;
    }

    private static Task<IResult> OnGetIndexCreationInfo()
    {
        var response = IndexCreationInformation.IndexCreationInfo;
        return Task.FromResult(Results.Ok(response));
    }

    private static async Task<IResult> OnPostChatAsync(
        [FromBody] ChatRequest request,
        [FromServices] ISwaggerAiAssistantService swaggerAiAssistantService,
        CancellationToken cancellationToken)
    {
        if (request is { History.Length: > 0 })
        {
            var response = await swaggerAiAssistantService.AskApi(request.LastUserQuestion);

            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async Task<IResult> OnPostClearAsync(
        [FromServices] ISwaggerMemoryManagerService swaggerMemoryManager,
        CancellationToken cancellationToken)
    {
        try
        {
            await swaggerMemoryManager.RemoveMemory();
            return Results.Ok();
        }
        catch(Exception)
        {
            return Results.BadRequest();
        }
    }

    [IgnoreAntiforgeryToken]
    private static async Task<IResult> OnPostDocumentAsync(
        [FromForm] IFormFileCollection files,
        [FromForm] string apiToken,
        [FromServices] ISwaggerMemoryManagerService swaggerMemoryManager,
        [FromServices] ILogger<AzureBlobStorageService> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Upload documents");

        var swaggerFile = files.FirstOrDefault();
        await using var stream = swaggerFile.OpenReadStream();

        await swaggerMemoryManager.UploadMemory(swaggerFile.FileName, stream, apiToken);

        //TODO
        var response = new UploadDocumentsResponse(new[] { "dummy.txt" });

        logger.LogInformation("Upload documents: {x}", response);

        return TypedResults.Ok(response);
    }


    private static IAsyncEnumerable<DocumentResponse> OnGetDocumentsAsync(
        [FromServices] IUploaderDocumentService service,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var documentsStream = service.GetDocuments(cancellationToken);
        return documentsStream;
    }

    private static async Task<IResult> OnPostSynchronizeAsync(
        [FromServices] IUploaderDocumentService service,
        CancellationToken cancellationToken)
    {
        await service.UploadToAzureIndex();
        return TypedResults.Ok();
    }
}
