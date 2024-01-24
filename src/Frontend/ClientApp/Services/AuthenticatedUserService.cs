using Microsoft.AspNetCore.Components.Authorization;
using Shared.TableEntities;

namespace ClientApp.Services;

public class AuthenticatedUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IUserApiClient _userApiClient;

    public AuthenticatedUserService(AuthenticationStateProvider authenticationStateProvider, IUserApiClient userApiClient)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _userApiClient = userApiClient;
    }

    public async Task<UserEntity> GetAuthenticatedUserAsync()
    {
        var identity = await GetAuthenticatedUserNameAsync();
        if(identity == null)
        {
            return null;
        }

        var users = await _userApiClient.GetAllUsers();

        return users.FirstOrDefault(x => x.Email == identity);
    }
    private async Task<string> GetAuthenticatedUserNameAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity.IsAuthenticated)  
        {  
	        var groupClaims = user.Claims.Where(c => c.Type == "groups").ToList();  
  
	        foreach (var claim in groupClaims)  
	        {  
		        Console.WriteLine($"Group ID: {claim.Value}");  
	        }  
        } 

        return user.Identity.IsAuthenticated ? user.Identity.Name : null;
    }
}
