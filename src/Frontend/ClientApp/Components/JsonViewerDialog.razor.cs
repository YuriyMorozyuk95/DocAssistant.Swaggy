using DialogResult = MudBlazor.DialogResult;

namespace ClientApp.Components;

public sealed partial class JsonViewerDialog
{
    private bool _isLoading = true;
    private string _pdfViewerVisibilityStyle => _isLoading ? "display:none;" : "display:default; overflow-y: scroll; overflow-x: scroll;";
    
    [Parameter] public required string FileName { get; set; }
    [Parameter] public required string BaseUrl { get; set; }
    [Parameter] public string OriginUri { get; set; }
    [Parameter] public string JsonContent { get; set; }

    public string Json { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;

        if(!string.IsNullOrWhiteSpace(BaseUrl))
        {
            var httpClient = new HttpClient();  
            JsonContent = await httpClient.GetStringAsync(new Uri(BaseUrl));
        }

        if(!string.IsNullOrWhiteSpace(JsonContent))
        {
            // Parse and format JSON  
            var parsedJson = JsonDocument.Parse(JsonContent);  
            var bytes = JsonSerializer.SerializeToUtf8Bytes(parsedJson.RootElement, new JsonSerializerOptions { WriteIndented = true });  
  
            // Convert bytes to string  
            Json = Encoding.UTF8.GetString(bytes)
                .Replace("\\n", "\n")
                .Replace("\\u0027", "'");  
        }

       
        _isLoading = false;
        await base.OnParametersSetAsync();
    }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    //TODO
    private void OnCloseClick() => Dialog.Close(DialogResult.Ok(true));
}
