using System.Diagnostics;

namespace DocAssistant.Ai.Services;

public interface ICurlExecutor
{
    Task<string> ExecuteCurl(string curl, TimeSpan timeOut = default);
}

public class CurlExecutor : ICurlExecutor
{
    public async Task<string> ExecuteCurl(string curl, TimeSpan timeOut = default)
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

            return result;
        }
        catch (TaskCanceledException)
        {
            process.Kill();
            return "Process timed out and was terminated";
        }
    }
}