using DocAssistant.Ai;
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
        api.MapPost("documents-file", OnPostFromFileDocumentAsync).DisableAntiforgery();

        api.MapPost("documents-url", OnPostFromUrlDocumentAsync).DisableAntiforgery();

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
            await Task.Delay(10000, cancellationToken);
            return Results.Ok();
        }
        catch (Exception)
        {
            return Results.BadRequest();
        }
    }

    [IgnoreAntiforgeryToken]
    private static async Task<IResult> OnPostFromFileDocumentAsync(
        [FromForm] IFormFileCollection files,
        [FromForm] string apiToken,
        [FromServices] ISwaggerMemoryManagerService swaggerMemoryManager,
        [FromServices] ILogger<AzureBlobStorageService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Upload documents");

            var swaggerFile = files.First();
            await using var stream = swaggerFile.OpenReadStream();

            _ = swaggerMemoryManager.UploadMemory(swaggerFile.FileName, stream, apiToken);
            await Task.Delay(3000, cancellationToken);

            var response = new UploadDocumentsResponse(new[] { swaggerFile.FileName });

            logger.LogInformation("Upload documents: {x}", response);

            return TypedResults.Ok(response);
        }
        catch (Exception e)
        {
            return Results.BadRequest(e);
        }
    }

    [IgnoreAntiforgeryToken]
    public static async Task<IResult> OnPostFromUrlDocumentAsync(
        [FromForm] string swaggerFileUrl,
        [FromForm] string apiToken,
        [FromServices] ISwaggerMemoryManagerService swaggerMemoryManager,
        [FromServices] ILogger<AzureBlobStorageService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Upload documents");

            using var httpClient = new HttpClient();
            var responseMessage = await httpClient.GetAsync(swaggerFileUrl, cancellationToken);
            responseMessage.EnsureSuccessStatusCode();

            var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
            var swaggerFile = Path.GetFileName(new Uri(swaggerFileUrl).LocalPath);

            string[] parts = swaggerFileUrl.Split('.');  
            string firstPartOfUrl = parts[1];

            var fileName = $"{firstPartOfUrl}-{swaggerFile}";

            _ = swaggerMemoryManager.UploadMemory(fileName, stream, apiToken);
            await Task.Delay(3000, cancellationToken);

            var response = new UploadDocumentsResponse(new[] { swaggerFile });

            logger.LogInformation("Upload documents: {x}", response);

            return TypedResults.Ok(response);
        }
        catch (Exception e)
        {
            return Results.BadRequest(e);
        }

    }

    private static async Task<IResult> OnGetDocumentsAsync(
        [FromServices] IDocumentStorageService service,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var response = await service.RetrieveOriginFiles().ToListAsync(cancellationToken: cancellationToken);

        return TypedResults.Ok(response);
    }
}
