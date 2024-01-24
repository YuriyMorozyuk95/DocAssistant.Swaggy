namespace MinimalApi.Services;

public class PromptFileService
{
    public static string ReadPromptsFromFile(string file)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Prompts", file);
        var prompt = File.ReadAllText(path);
        return prompt;
    }

    public static string ReadPromptsFromFile(string file, IDictionary<string, string> keyValue)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Prompts", file);
        var prompt = File.ReadAllText(path);

        foreach(var (key, value) in keyValue)
        {
            prompt = prompt.Replace(key, value);
        }
        
        return prompt;
    }

    public static void UpdatePromptsToFile(string file, string content)  
    {  
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Prompts", file);  
        File.WriteAllText(path, content);  
    }  

}
