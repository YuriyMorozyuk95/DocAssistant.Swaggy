namespace Shared.Models;

public class UserGroup
{
    public string Id { get; set; }

    public string Name { get; set; }

    public override string ToString() => Name ?? base.ToString();
}
