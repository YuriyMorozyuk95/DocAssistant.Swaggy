using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi.Tests.AzurePing;

public class DocumentAnalysisClientTest(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CanReachFormRecognizerEndpoints()
    {
        // Arrange
        var documentAnalysisClient = factory.Services.GetRequiredService<DocumentAnalysisClient>();

        string filePath = "Assets/test.jpg";
        using var fileStream = File.OpenRead(filePath);

        // Analyze the file
        var analyzeResultResponse = await documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", fileStream);

        Assert.NotNull(analyzeResultResponse);
    }
}