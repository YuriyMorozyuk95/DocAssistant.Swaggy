using Azure.Core;
using Azure.Identity;
using GunvorCopilot.Backend.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using MinimalApi.Services;

namespace MinimalApi.Tests;

public class GraphServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GraphServiceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGroupsAsync_ReturnsGroups()
    {
        // Arrange
        var graphGroupService = _factory.Services.GetRequiredService<IApplicationGraphService>() as GraphService;
        var objectId = "bd670958-329e-444a-8f5d-71bbfaaa8d61";

        // Act
        //var groups = await graphGroupService.GetAllUserGroup(objectId);
        var groups = await graphGroupService.GetUserApplicationAssignedGroups(objectId);

        // Assert
        Assert.NotNull(groups);
        Assert.True(groups.Any());
    }

	[Fact]
	public async Task BlackstarLightTest()
	{
		var scopes = new string[] { "api://4fe00117-8309-4d76-b913-8fdad7b3e2b7/.default" };

		var confidentialClient = ConfidentialClientApplicationBuilder
			.Create("13a6038e-c720-4a83-b06e-37cbf189fc37")
			.WithClientSecret("W.W8Q~.s1wUPAjwlmpf-CcPBeMBaW2Tutt~HWayo")
			.WithAuthority(new Uri("https://login.microsoftonline.com/11980ae3-cae6-4552-94d2-5ad474856f9e"))
			.Build();

		var result = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync();

		var token = result.AccessToken;
	}

}