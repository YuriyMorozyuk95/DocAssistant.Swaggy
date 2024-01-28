using DocAssistant.Ai.Model;

using System.Diagnostics;
using System.Text.Json;

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
        try
        {
            (curl, filePath) = await PutJsonToFile(curl);

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

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(result);
                apiResponse.IsSuccess = apiResponse.Code >= 200 && apiResponse.Code <= 299;
                apiResponse.Result = result;

                return apiResponse;
            }
            catch (TaskCanceledException)
            {
                process.Kill();
                return new ApiResponse { Result = "Process timed out and was terminated" };
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

    private async Task<(string curl, string filePath)> PutJsonToFile(string curl)
    {
        string[] parts = curl.Split(" -d ");

        string curlCommand = parts[0];
        string jsonBody = parts[1];
        jsonBody = jsonBody.Replace("'", string.Empty);

        string tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, jsonBody);

        curlCommand += $" -d @{tempFile}";

        return (curlCommand, tempFile);
    }
}
//ProcessStartInfo startInfo = new ProcessStartInfo()  
//{  
//    FileName = @"C:\Users\ymoroziuk\AppData\Local\Programs\Git\git-bash.exe",  // Update this path if necessary  
//    Arguments = "-c \"" + curl + "\"",  
//    RedirectStandardOutput = true  
//};