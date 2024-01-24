using GunvorCopilot.Backend.Core;

namespace MinimalApi.Services;

public interface IUserGroupService
{
    Task<IEnumerable<UserGroup>> GetMyGroups();
}

public class UserGroupService : IUserGroupService
{
    private readonly IDelegatedGraphService _applicationGraphService;

    public UserGroupService(IDelegatedGraphService applicationGraphService)
    {
        _applicationGraphService = applicationGraphService;
    }

    public async Task<IEnumerable<UserGroup>> GetMyGroups()
    {
        var graphGroups = await _applicationGraphService.GetMyGroups();
        var permissionGroup = graphGroups.Select(g => new UserGroup
        {
            Id = g.Id,
            Name = g.DisplayName,
        });

        return permissionGroup;
    }
}
