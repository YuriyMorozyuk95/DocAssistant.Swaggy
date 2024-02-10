namespace Shared.Models.Swagger;

public class SwaggerCompletionInfo
{
    public string FinalResult { get; set; }
    public string Endpoint { get; set; }
    public int CompletionTokens { get; set; }
    public int PromptTokens { get; set; }
    public int TotalTokens { get; set; }
    public ApiResponse Response { get; set; }
    public SwaggerDocument SwaggerDocument { get; set; }
    public bool IsSuccessful => FinalResult != null;
}