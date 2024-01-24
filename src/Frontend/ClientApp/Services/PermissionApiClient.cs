using Microsoft.Extensions.Logging;

using System.Net;

using static MudBlazor.CategoryTypes;

namespace ClientApp.Services;

public interface IPermissionApiClient
{
    Task<IEnumerable<UserGroup>> GetUserGroups();
}

public class PermissionApiClient : IPermissionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PermissionApiClient> _logger;

    public PermissionApiClient(HttpClient httpClient, ILogger<PermissionApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<UserGroup>> GetUserGroups()
    {
        try
        {
            var userGroupResponse = await _httpClient.GetAsync("api/userGroups");

            if (userGroupResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Failed authenticate");
                return new List<UserGroup>();
            }
            userGroupResponse.EnsureSuccessStatusCode();
            return await userGroupResponse.Content.ReadFromJsonAsync<IEnumerable<UserGroup>>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get user groups");
            throw;
        }
    }

    public async Task<UserGroup> GetUserGroupById(string userGroupId)
    {
        var response = await _httpClient.GetAsync($"api/userGroups/{userGroupId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserGroup>();
    }
}
