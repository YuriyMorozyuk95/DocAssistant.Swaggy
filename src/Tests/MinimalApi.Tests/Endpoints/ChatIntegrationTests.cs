using ClientApp.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.Models;

namespace MinimalApi.Tests.Endpoints
{
    public class ChatIntegrationTests : IClassFixture<WebApplicationFactory<Program>>  
    {  
        private readonly WebApplicationFactory<Program> _factory;  
        private readonly ApiClient _client;  
  
        public ChatIntegrationTests(WebApplicationFactory<Program> factory)  
        {  
            _factory = factory;
            _client = new ApiClient(_factory.CreateClient());
        }  
  
        [Fact]  
        public async Task Post_Chat_ReturnsOkResponse()  
        {  
            // Arrange  
            var request = new ChatRequest(new ChatTurn[] { new("Could you remove pet in store with id 11?"), }, Approach.RetrieveThenRead);
  
            // Act  
            var response = await _client.ChatToApiConversationAsync(request);  
  
            // Assert  
            Assert.NotNull(response);  
        }  
    }
}
