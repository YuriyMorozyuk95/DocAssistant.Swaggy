namespace Shared.Models;

public class SupportingContent(string title, string content, string originUri = null, string[] userGroups = null)
{
	public string Title { get; set; } = title;
	public string Content { get; set; } = content;
	public string OriginUri { get; set; } = originUri;
	public string[] UserGroups { get; set; } = userGroups ?? Array.Empty<string>();
}