namespace ClientApp.Models;

public class CitationDetails  
{  
    public string Name { get; set; }  
    public string BaseUrl { get; set; }  
    public int Number { get; set; }
    public string OriginUri { get; set; }
  
    public CitationDetails(string name, string baseUrl, int number = 0, string originUri = null)  
    {  
        Name = name;  
        BaseUrl = baseUrl;
        Number = number;
        OriginUri = originUri;
    }  
}  

