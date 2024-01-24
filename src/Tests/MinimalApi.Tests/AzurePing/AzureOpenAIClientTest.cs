using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi.Tests.AzurePing;

public class AzureOpenAIClientTest(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CanReachFormAzureOpenAIEndpoints()
    {
        var client = factory.Services.GetRequiredService<OpenAIClient>();

        Response<ChatCompletions> responseWithoutStream = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                DeploymentName = "docAssistant-gpt-4-32k",
                Messages =
                {
                    new ChatRequestSystemMessage(@"You are an AI assistant that helps people find information."),
                    new ChatRequestUserMessage(@"Hello how are you?"),
                },
                Temperature = (float)0.5,
                MaxTokens = 800,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            });


        var response = responseWithoutStream.Value;
        Assert.NotNull(response);
    }
}