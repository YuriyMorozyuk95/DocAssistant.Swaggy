using Shared;

using DialogParameters = MudBlazor.DialogParameters;
using IDialogService = MudBlazor.IDialogService;

namespace ClientApp.Pages;

public sealed partial class Documents : IDisposable
{
    private const long MaxIndividualFileSize = 1_024L * 1_024;

    private MudForm _form = null!;
    private MudFileUpload<IReadOnlyList<IBrowserFile>> _fileUpload = null!;
    private Task _getDocumentsTask = null!;
    private bool _isLoadingDocuments = false;
    private bool _isIndexUploading = false;
    private string _filter = "";
    private IndexCreationInfo _indexCreationInfo = new IndexCreationInfo();
    private Timer _timer;
    private IEnumerable<UserGroup> _selectedUserGroupsForDoc = new List<UserGroup>();

    // Store a cancelation token that will be used to cancel if the user disposes of this component.
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<DocumentResponse> _documents = new();

    [Inject]
    public required ApiClient Client { get; set; }

    //TODO
    [Inject]
    public required IDialogService Dialog { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required ILogger<Index> Logger { get; set; }

    [Inject]
    public required IJSRuntime JsRuntime { get; set; }

    private bool FilesSelected => _fileUpload is { Files.Count: > 0 };
    public string ApiToken { get; set; }

    protected override void OnInitialized()
    {
        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _getDocumentsTask = GetDocumentsAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        _timer = new Timer(async _ => await LoadIndexCreationInfoAsync(), null, 0, 5000);

        await LoadIndexCreationInfoAsync();
    }

    private async Task LoadIndexCreationInfoAsync()
    {
        try
        {
            _indexCreationInfo = await Client.GetIndexCreationInfoAsync(); // Call your method to fetch the IndexCreationInfo
        }
        catch (Exception e)
        {
            _indexCreationInfo = new IndexCreationInfo()
            {
                LastIndexErrorMessage = e.Message,
            };
        }
        finally
        {
            StateHasChanged(); // Notify Blazor that the state has changed and UI needs to be updated 
        }
    }

    private bool OnFilter(DocumentResponse document) => document is not null
                                                        && (string.IsNullOrWhiteSpace(_filter) || document.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase));

    private async Task GetDocumentsAsync()
    {
        _isLoadingDocuments = true;

        try
        {
            _documents.Clear();
            var documents =
                await Client.GetDocumentsAsync(_cancellationTokenSource.Token);

            foreach (var document in documents)
            {
                _documents.Add(document);
            }
        }
        catch(Exception e)
        {
            throw;
        }
        finally
        {
            _isLoadingDocuments = false;
            StateHasChanged();
        }
    }

    private async Task SubmitFilesForUploadAsync()
    {
        if (_fileUpload is { Files.Count: > 0 })
        {
            var cookie = await JsRuntime.InvokeAsync<string>("getCookie", "XSRF-TOKEN");

            var result = await Client.UploadDocumentsAsync(
                _fileUpload.Files.First(), ApiToken);

            ApiToken = string.Empty;

            Logger.LogInformation("Result: {x}", result);

            if (result.IsSuccessful)
            {
                Snackbar.Add(
                    $"Uploaded {result.UploadedFiles.Length} documents.",
                    Severity.Success,
                    static options =>
                    {
                        options.ShowCloseIcon = true;
                        options.VisibleStateDuration = 10_000;
                    });

                await _fileUpload.ResetAsync();
                if(_selectedUserGroupsForDoc is List<UserGroup> list)
                {
                    list.Clear();
                }
                await GetDocumentsAsync();
            }
            else
            {
                Snackbar.Add(
                    result.Error,
                    Severity.Error,
                    static options =>
                    {
                        options.ShowCloseIcon = true;
                        options.VisibleStateDuration = 10_000;
                    });
            }
        }
    }

    //TODO
    private void OnShowDocument(DocumentResponse document) => Dialog.Show<JsonViewerDialog>(
            $"📄 {document.Name}",
            new DialogParameters
            {
                [nameof(JsonViewerDialog.FileName)] = document.Name,
                [nameof(JsonViewerDialog.BaseUrl)] = document.Url,
                    //document.Url.ToString().Replace($"/{document.Name}", ""),
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });

    private async Task CleanUpDocuments()
    {
        try
        {
            _isLoadingDocuments = true;
            var result = await Client.ClearMemory();
            if (result.IsSuccessful)
            {
                Snackbar.Add(
                    $"Memory successfully cleaned, please wait 10 sec to refresh container.",
                    Severity.Success,
                    static options =>
                    {
                        options.ShowCloseIcon = true;
                        options.VisibleStateDuration = 10_000;
                    });

                await _fileUpload.ResetAsync();
                ApiToken = string.Empty;

                _documents.Clear();
            }
            else
            {
                Snackbar.Add(
                    result.Error,
                    Severity.Error,
                    static options =>
                    {
                        options.ShowCloseIcon = true;
                        options.VisibleStateDuration = 10_000;
                    });
            }
        }
        finally
        {
            _isLoadingDocuments = false;
        }
    }
    public void Dispose()
    {
        _timer?.Dispose();
        _cancellationTokenSource.Cancel();
    }
}
