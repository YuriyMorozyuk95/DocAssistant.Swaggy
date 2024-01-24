using GunvorCopilot.Data.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MinimalApi.Tests;

public class UserRepositoryServiceTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CanConnectToAzureCosmosDb()
    {
        // Arrange
        var uploaderDocumentService = factory.Services.GetRequiredService<IUserRepository>();

        // Act
        var documents = await uploaderDocumentService.GetAllUsersAsync();

        // Assert
        Assert.NotNull(documents);
    }
}