using System.Text;
using ClientApp.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace MinimalApi.Tests.Endpoints;

public class DocumentUploadIntegrationTests : IClassFixture<WebApplicationFactory<Program>>  
{  
    private readonly WebApplicationFactory<Program> _factory;  
    private readonly ApiClient _client;  
  
    public DocumentUploadIntegrationTests(WebApplicationFactory<Program> factory)  
    {  
        _factory = factory;  
        _client = new ApiClient(_factory.CreateClient());  
    }  
  
    [Fact]  
    public async Task Post_DocumentUpload_ReturnsOkResponse()  
    {  
        // Arrange  
        var fileMock = new Mock<IBrowserFile>();  
        fileMock.Setup(_ => _.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("This is a dummy file")));  
        fileMock.Setup(_ => _.ContentType).Returns("text/plain");  
        fileMock.Setup(_ => _.Name).Returns("dummy.txt");  
  
        var apiToken = "YourApiToken";  
        var cookie = "YourCookie";  
  
        // Act  
        var response = await _client.UploadDocumentsAsync(fileMock.Object, apiToken);  
  
        // Assert  
        Assert.NotNull(response);  
        Assert.True(response.IsSuccessful);  
    }  
}