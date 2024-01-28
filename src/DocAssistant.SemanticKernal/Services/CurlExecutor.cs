using Azure;
using DocAssistant.Ai.Model;
using System.Diagnostics;
using System.Text.Json;

using static DocAssistant.Ai.Services.CurlExecutor;

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

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = "/C" + curl,
            RedirectStandardOutput = true
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
}