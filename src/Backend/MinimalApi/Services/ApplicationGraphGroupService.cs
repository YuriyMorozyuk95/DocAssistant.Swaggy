using GunvorCopilot.Backend.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.CheckMemberGroups;

namespace MinimalApi.Services
{
    public class GraphService : IApplicationGraphService, IDelegatedGraphService
    {
        private readonly GraphServiceClient _graphService;
        private readonly ILogger<GraphService> _logger;
        private readonly string _appId;

        public GraphService(GraphServiceClient graphService, ILogger<GraphService> logger, IConfiguration configuration)
        {
	        _graphService = graphService;
	        _logger = logger;
			_appId = configuration["AzureAd:ClientId"];
        }

        public async Task<IEnumerable<Group>> GetAllGroups()
        {
            var groups = await _graphService.Groups.GetAsync();
            if (groups == null)
            {
                return Enumerable.Empty<Group>();
            }

            return TranslateGroup(groups);
        }

      

        public async Task<IEnumerable<Group>> GetAllUserGroup(string userId)
		{
			try
			{
				var userGroups = new List<Group>();  
				var groups = await _graphService.Groups.GetAsync();  
  
				foreach (var group in groups.Value)  
				{  
					var members = await _graphService.Groups[group.Id].Members.GetAsync();
  
					if (members.Value.Any(member => member.Id == userId))  
					{  
						userGroups.Add(group);  
					}  
				}

				return userGroups;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Enumerable.Empty<Group>();
			}
		}

        public async Task<IEnumerable<string>> GetUserApplicationAssignedGroups(string userId)
        {
			// Replace 'app-id' with your application's id  
			var appRoleAssignments = await _graphService.ServicePrincipals[_appId].AppRoleAssignedTo
				.GetAsync(x => {}/*x.QueryParameters.Expand = new string[] { "displayName" }*/);

			List<string> groupIds = new List<string>();  
  
	        foreach (var assignment in appRoleAssignments.Value)  
	        {  
		        if (assignment.PrincipalType == "Group")  
		        {  
			        groupIds.Add(assignment.PrincipalId.ToString());  
		        }  
	        }  
  
	        var checkMemberGroupsResult = await _graphService.Users[userId].CheckMemberGroups.PostAsCheckMemberGroupsPostResponseAsync(new CheckMemberGroupsPostRequestBody
            {
                GroupIds = groupIds,
            });  
  
            return checkMemberGroupsResult.Value;
		}


		//TODO add parameter with group ids
		public async Task<IEnumerable<Group>> GetMyGroups()
        {
            try
            {
                var groups = await _graphService.Me.TransitiveMemberOf.GraphGroup.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });
                if (groups == null)
                {
                    return Enumerable.Empty<Group>();
                }

                return groups.Value!.Select(g => new Group { Id = g.Id, DisplayName = g.DisplayName });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Enumerable.Empty<Group>();
            }
        }

        private static IEnumerable<Group> TranslateGroup(GroupCollectionResponse groups)
        {
	        return groups.Value!.Select(g => new Group { Id = g.Id, DisplayName = g.DisplayName });
        }
    }
}
