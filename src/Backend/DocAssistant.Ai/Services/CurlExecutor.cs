using System.Diagnostics;
using System.Text.Json;
using Shared.Models.Swagger;

namespace DocAssistant.Ai.Services;

public interface ICurlExecutor
{
    Task<ApiResponse> ExecuteCurl(string curl, TimeSpan timeOut = default);
}

public class CurlExecutor : ICurlExecutor
{
    public async Task<ApiResponse> ExecuteCurl(string curl, TimeSpan timeOut = default)
    {
        if (timeOut == default)
        {
            timeOut = TimeSpan.FromSeconds(5);
        }
        string filePath = null;
        string json = null;
        try
        {
            //if (curl.Contains("-d"))
            //{
            //    curl = await FormatJsonInCurl(curl);
            //}

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/C" + curl,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            using var cts = new CancellationTokenSource(timeOut);
            Process process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true };

            process.Start();

            try
            {
                await process.WaitForExitAsync(cts.Token);
                string result = await process.StandardOutput.ReadToEndAsync(cts.Token);

                var apiResponse = CreateApiResponseFromResult(result);
                apiResponse.Request = curl;

                return apiResponse;
            }
            catch (TaskCanceledException)
            {
               process.Kill();
               var message = "Process timed out and was terminated";

               return Error(message);
            }
        }
        finally
        {
            if (filePath != null)
            {
                File.Delete(filePath);
            }
        }
    }

    private ApiResponse Error(string message)
    {
        return new ApiResponse { IsSuccess = false, Code = 400, Message = message, Result = message, };
    }

    public ApiResponse CreateApiResponseFromResult(string result)
    {
        ApiResponse apiResponse = new ApiResponse();
        if(string.IsNullOrWhiteSpace(result))
        {
           return Error("Empty response");
        }

        if (CanDeserializeToApiResponse(result))
        {
            var options = new JsonSerializerOptions  
            {  
                AllowTrailingCommas = true,  
                PropertyNameCaseInsensitive = true,
            };

            apiResponse = JsonSerializer.Deserialize<ApiResponse>(result, options);
            apiResponse.IsSuccess = apiResponse.Code >= 200 && apiResponse.Code <= 299;
        }
        else
        {
            apiResponse.IsSuccess = true;
            apiResponse.Code = 200;
            apiResponse.Message = result;
        }

        apiResponse.Result = result;

        return apiResponse;
    }

    public async Task<string> FormatJsonInCurl(string curl)
    {
        string[] parts = curl.Split(" -d ");

        string curlCommand = parts[0];
        string jsonBody = parts[1];
        jsonBody = jsonBody.Replace("'", string.Empty);

        JsonDocument document = JsonDocument.Parse(jsonBody);  
        string cleanJsonString = document.RootElement.GetRawText(); 

        curlCommand += $" -d @'{cleanJsonString}'";

        return curlCommand;
    }

    //TODO use logger instead of console
    public bool CanDeserializeToApiResponse(string jsonString)    
    {    
        try  
        {
            var options = new JsonDocumentOptions  
            {  
                AllowTrailingCommas = true,  
            };
            JsonDocument doc = JsonDocument.Parse(jsonString, options);
            JsonElement root = doc.RootElement;  
  
            // Check if keys "code" and "message" exist  
            if (root.TryGetProperty("code", out JsonElement codeElement) && root.TryGetProperty("message", out JsonElement messageElement))  
            {  
                // Check if "code" is integer and "message" is string  
                if (codeElement.ValueKind == JsonValueKind.Number && messageElement.ValueKind == JsonValueKind.String)  
                {  
                    return true;  
                }  
            }  
  
            return false;  
        }  
        catch (JsonException jex)  
        {  
            // Exception in parsing json  
            Console.WriteLine(jex.Message);  
            return false;  
        }  
        catch (Exception ex) // Some other exception  
        {  
            Console.WriteLine(ex.ToString());  
            return false;  
        }  
    }  
}