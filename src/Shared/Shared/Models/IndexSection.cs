namespace Shared.Models;

public readonly struct IndexSection
{
    public const string UserGroupsFieldName = "userGroups";

    public string Id { get; }
    public string Content { get; }
    public string SourcePage { get; }
    public string SourceFile { get; }
    public string[] UserGroups { get; }

    public IndexSection(string id, string content, string sourcePage, string sourceFile, string[] userGroups)
    {
        Id = id;
        Content = content;
        SourcePage = sourcePage;
        SourceFile = sourceFile;
        UserGroups = userGroups;
    }
}
