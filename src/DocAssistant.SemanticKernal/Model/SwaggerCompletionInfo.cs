namespace DocAssistant.Ai.Model;

public class SwaggerCompletionInfo
{
    public string FinalleResult { get; set; }
    public string Curl { get; set; }
    public ApiResponse Response { get; set; }
    public int CompletionTokens { get; set; }
    public int PromptTokens { get; set; }
    public int TotalTokens { get; set; }
}