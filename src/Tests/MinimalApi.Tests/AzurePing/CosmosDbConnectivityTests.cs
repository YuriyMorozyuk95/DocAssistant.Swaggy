using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi.Tests.AzurePing;

public class CosmosDbConnectivityTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CanConnectToAzureCosmosDb()
    {
        // Arrange
        var server = factory.Server;
        var configuration = server.Services.GetRequiredService<IConfiguration>();

        // Act
        var cosmosClient = server.Services.GetRequiredService<CosmosClient>();

        var cosmosDbName = configuration["CosmosDB:Name"];
        await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbName);
    }
}