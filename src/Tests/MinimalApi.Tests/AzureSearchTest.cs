using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Services;

namespace MinimalApi.Tests;

public class AzureSearchTest(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CanEnsureSearchIndex()
    {
        // Arrange
        var uploaderDocumentService = factory.Services.GetRequiredService<IAzureSearchEmbedService>();

        await uploaderDocumentService.EnsureSearchIndex("test");
    }

    [Fact]
    public async Task CanQuerySearchIndex()
    {
        // Arrange
        var searchClient = factory.Services.GetRequiredService<SearchClient>();

        var top =3;
        SearchOptions searchOption = new SearchOptions
            {
                Size = top,
            };

        var embedding = new float[1536];
        var vectorQuery = new VectorizedQuery(embedding)
        {
            // if semantic ranker is enabled, we need to set the rank to a large number to get more
            // candidates for semantic reranking
            KNearestNeighborsCount = top,
        };
        vectorQuery.Fields.Add("embedding");
        searchOption.VectorSearch = new();
        searchOption.VectorSearch.Queries.Add(vectorQuery);

        var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
            null, searchOption);

        Assert.NotNull(searchResultResponse);
    }
}