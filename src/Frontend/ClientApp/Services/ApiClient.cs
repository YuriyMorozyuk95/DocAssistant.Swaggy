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

    public async Task<ImageResponse> RequestImageAsync(PromptRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/images", request, SerializerOptions.Default);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImageResponse>();
    }

    public async Task<UploadDocumentsResponse> UploadDocumentsAsync(
        IReadOnlyList<IBrowserFile> files,
        UserGroup[] userGroups,
        long maxAllowedSize,
        string cookie)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            foreach (var file in files)
            {
                // max allow size: 10mb
                var maxSize = maxAllowedSize * 1024 * 1024;
#pragma warning disable CA2000 // Dispose objects before losing scope
                var fileContent = new StreamContent(file.OpenReadStream(maxSize));
#pragma warning restore CA2000 // Dispose objects before losing scope
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                content.Add(fileContent, file.Name, file.Name);
            }

            // Add userGroups
            var serializedUserGroups = JsonSerializer.Serialize(userGroups);
            var userGroupContent = new StringContent(serializedUserGroups, Encoding.UTF8, "application/json");
            content.Add(userGroupContent, "userGroupContent");

            // set cookie
            content.Headers.Add("X-CSRF-TOKEN-FORM", cookie);
            content.Headers.Add("X-CSRF-TOKEN-HEADER", cookie);

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

    public Task<AnswerResult<ChatRequest>> ChatConversationAsync(ChatRequest request) => PostRequestAsync(request, "api/chat");
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

    public async Task SynchronizeDocumentsAsync(string cookie)
    {
        using var content = new MultipartFormDataContent();

        // set cookie
        content.Headers.Add("X-CSRF-TOKEN-FORM", cookie);
        content.Headers.Add("X-CSRF-TOKEN-HEADER", cookie);

        var response = await _httpClient.PostAsync("api/synchronize", content);

        response.EnsureSuccessStatusCode();
    }

    private async Task<AnswerResult<TRequest>> PostRequestAsync<TRequest>(
        TRequest request, string apiRoute) where TRequest : ApproachRequest
    {
        var result = new AnswerResult<TRequest>(
            IsSuccessful: false,
            Response: null,
            Approach: request.Approach,
            Request: request);

        var json = JsonSerializer.Serialize(
            request,
            SerializerOptions.Default);

        using var body = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(apiRoute, body);

        if (response.IsSuccessStatusCode)
        {
            var answer = await response.Content.ReadFromJsonAsync<ApproachResponse>();
            return result with
            {
                IsSuccessful = answer is not null,
                Response = answer
            };
        }
        else
        {
            var answer = new ApproachResponse(
                $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}",
                null,
                Array.Empty<SupportingContent>(),
                string.Empty,
                Array.Empty<string>(),
                "Unable to retrieve valid response from the server.");

            return result with
            {
                IsSuccessful = false,
                Response = answer
            };
        }
    }

    public async Task<CopilotPromptsRequestResponse> GetCopilotPromptsAsync()  
    {  
        var response = await _httpClient.GetAsync("api/copilot-prompts");  
        response.EnsureSuccessStatusCode();  
        return (await response.Content.ReadFromJsonAsync<CopilotPromptsRequestResponse>())!;  
    }  
  
    public async Task PostCopilotPromptsServerDataAsync(CopilotPromptsRequestResponse updatedData)  
    {  
        var response = await _httpClient.PostAsJsonAsync("api/copilot-prompts", updatedData);  
        response.EnsureSuccessStatusCode();  
    }

    public async Task<IndexCreationInfo> GetIndexCreationInfoAsync()  
    {  
        var response = await _httpClient.GetAsync("api/synchronize-status");  
        response.EnsureSuccessStatusCode();
        var stringResponse = await response.Content.ReadAsStringAsync();
        return (await response.Content.ReadFromJsonAsync<IndexCreationInfo>())!;  
    }

    public async Task<string> UploadAvatarAsync(IBrowserFile file, string cookie)
    {
        using var content = new MultipartFormDataContent();
        // max allow size: 10mb
        var maxSize = 10 * 1024 * 1024;
        var fileContent = new StreamContent(file.OpenReadStream(maxSize));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        content.Add(fileContent, "file", file.Name);

        // set cookie
        content.Headers.Add("X-CSRF-TOKEN-FORM", cookie);
        content.Headers.Add("X-CSRF-TOKEN-HEADER", cookie);

        var response = await _httpClient.PostAsync("api/upload-avatar", content);

        response.EnsureSuccessStatusCode();

        var stringResponse = await response.Content.ReadAsStringAsync();
        return stringResponse;
    }


}
