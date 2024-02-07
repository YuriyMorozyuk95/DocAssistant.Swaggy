using System.Net.Http.Headers;

using Shared;
using Shared.Models.Swagger;

using SupportingContent = Shared.Models.SupportingContent;

namespace ClientApp.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UploadDocumentsResponse> UploadDocumentsAsync(
        IBrowserFile file,
        string apiToken)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            // max allow size: 10mb
            var maxSize = 10_000_000;
#pragma warning disable CA2000 // Dispose objects before losing scope
            var fileContent = new StreamContent(file.OpenReadStream(maxSize));
#pragma warning restore CA2000 // Dispose objects before losing scope
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, file.Name, file.Name);

            var apiTokenContent = new StringContent(apiToken, Encoding.UTF8, "plain/text");
            content.Add(apiTokenContent, "apiToken");

            //TODO do not await , as it long runnign task
            var response = await _httpClient.PostAsync("api/documents", content);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<UploadDocumentsResponse>();

            return result
                   ?? UploadDocumentsResponse.FromError(
                       "Unable to upload files, unknown error.");
        }
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
    }


    public async IAsyncEnumerable<DocumentResponse> GetDocumentsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("api/documents", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            await foreach (var document in
                JsonSerializer.DeserializeAsyncEnumerable<DocumentResponse>(stream, options, cancellationToken))
            {
                if (document is null)
                {
                    continue;
                }

                yield return document;
            }
        }
    }

    public async Task<SwaggerCompletionInfo> ChatToApiConversationAsync(ChatRequest request)
    {
        //TODO: Implement the logic to handle error response from the server
        //var result = new AnswerResult<ChatRequest>(
        //    IsSuccessful: false,
        //    Response: null,
        //    Approach: request.Approach,
        //    Request: request);

        var json = JsonSerializer.Serialize(
            request,
            SerializerOptions.Default);

        using var body = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/chat", body);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<SwaggerCompletionInfo>();
        }
        //else
        //{
        //    var answer = new ApproachResponse(
        //        $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}",
        //        null,
        //        Array.Empty<SupportingContent>(),
        //        string.Empty,
        //        Array.Empty<string>(),
        //        "Unable to retrieve valid response from the server.");

        //    return result with
        //    {
        //        IsSuccessful = false,
        //        Response = answer
        //    };
        //}
        return new SwaggerCompletionInfo();
    }

    public async Task<ServiceResponse> ClearMemory()
    {
        try
        {
            //TODO refresh document after complite
            var response = await _httpClient.DeleteAsync("api/clear");

            response.EnsureSuccessStatusCode();

            return new ServiceResponse();
        }
        catch (Exception ex)
        {
            return ServiceResponse.FromError(ex.ToString());
        }
    }

    public async Task<IndexCreationInfo> GetIndexCreationInfoAsync()
    {
        var response = await _httpClient.GetAsync("api/synchronize-status");
        response.EnsureSuccessStatusCode();
        var stringResponse = await response.Content.ReadAsStringAsync();
        return (await response.Content.ReadFromJsonAsync<IndexCreationInfo>())!;
    }
}
