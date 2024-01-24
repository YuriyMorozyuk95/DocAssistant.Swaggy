using Microsoft.AspNetCore.Components.Routing;

namespace ClientApp.Components;

public sealed partial class SettingsPanel : IDisposable
{
    private bool _open;
    private SupportedSettings _supportedSettings;

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required AuthenticatedUserService AuthenticatedUserService { get; set; }
    [Inject] public required IPermissionApiClient PermissionApiClient { get; set; }

    [Inject]
    public IUserApiClient UserApiClient { get; set; }

    [Parameter]
    public required CopilotPromptsRequestResponse CopilotPrompts { get; set; } = new();  

    public RequestSettingsOverrides Settings { get; } = new();

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public bool Open
#pragma warning restore BL0007 // This is required for proper event propagation
    {
        get => _open;
        set
        {
            if (_open == value)
            {
                return;
            }

            _open = value;
            OpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public EventCallback<bool> UpdateButtonClicked { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var user = await AuthenticatedUserService.GetAuthenticatedUserAsync();

        if (user != null)
        {
            Settings.Overrides.SelectedUserGroups = user.UserGroups;
        }
        else
        {
            Settings.Overrides.SelectedUserGroups = new List<UserGroup>();
        }

        await base.OnInitializedAsync();
    }

    protected override void OnInitialized() => Nav.LocationChanged += HandleLocationChanged;

    private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
    {
        var url = new Uri(e.Location);
        var route = url.Segments.LastOrDefault();
        _supportedSettings = route switch
        {
            "ask" => SupportedSettings.Ask,
            "chat" => SupportedSettings.Chat,
            _ => SupportedSettings.All
        };
    }

    public void Dispose() => Nav.LocationChanged -= HandleLocationChanged;

    private async Task OnClickBroadcastAsync()
    {
        if (UpdateButtonClicked.HasDelegate)
        {
            await UpdateButtonClicked.InvokeAsync();
        }
    }
}

public enum SupportedSettings
{
    All,
    Chat,
    Ask
};
