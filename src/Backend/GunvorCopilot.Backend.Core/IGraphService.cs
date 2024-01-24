using Microsoft.Graph.Models;

namespace GunvorCopilot.Backend.Core;

public interface IApplicationGraphService
{
    Task<IEnumerable<Group>> GetAllGroups();
}

public interface IDelegatedGraphService
{
    Task<IEnumerable<Group>> GetMyGroups();
}
