using DialogResult = MudBlazor.DialogResult;

namespace ClientApp.Components;

public sealed partial class PdfViewerDialog
{
    private bool _isLoading = true;
    private string _pdfViewerVisibilityStyle => _isLoading ? "display:none;" : "display:default;";
    
    [Parameter] public required string FileName { get; set; }
    [Parameter] public required string BaseUrl { get; set; }
    [Parameter] public string OriginUri { get; set; }

    public string Json { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var httpClient = new HttpClient();  
        var json = await httpClient.GetStringAsync(new Uri(BaseUrl));
        string unescapedJson = JsonSerializer.Serialize<string>(json);
        Json = unescapedJson;
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
