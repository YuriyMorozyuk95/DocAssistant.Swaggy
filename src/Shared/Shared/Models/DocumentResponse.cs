namespace Shared.Models;

public class DocumentResponse  
{  
    public string Name { get; init; }  
    public string ContentType { get; init; }  
    public long Size { get; init; }  
    public DateTimeOffset? LastModified { get; init; }  
    public string Url { get; init; }  
}  

