using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Services;

namespace MinimalApi.Tests.AzurePing;

public class UploaderDocumentServiceTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CanConnectToAzureStorage()
    {
        // Arrange
        var uploaderDocumentService = factory.Services.GetRequiredService<IUploaderDocumentService>();

        // Act
        var documents = await uploaderDocumentService.GetDocuments().ToListAsync();

        // Assert
        Assert.NotNull(documents);
    }
}