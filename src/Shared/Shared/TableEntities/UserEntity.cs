using Newtonsoft.Json;
using Shared.Models;

namespace Shared.TableEntities;
public class UserEntity
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string PartitionKey { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
    public int AccountId { get; set; }
    public bool IsAdmin { get; set; } = false;
    public string ImageUrl { get; set; }

    public IEnumerable<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}
