using GunvorCopilot.Data.Interfaces;

namespace MinimalApi.Extensions;

internal static class UserGroupEndpointsExtensions
{
    internal static void MapUserGroupManagementApi(this RouteGroupBuilder api)  
    {  
        // Permission management endpoints    
        api.MapGet("userGroups", OnGetUserGroups);
    }  
  
    private static async Task OnGetUserGroups(HttpContext context, IUserGroupService userGroupService)  
    {
        var userGroups = await userGroupService.GetMyGroups();
        await context.Response.WriteAsJsonAsync(userGroups);
    }
}
