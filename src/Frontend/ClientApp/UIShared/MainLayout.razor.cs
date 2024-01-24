using Microsoft.AspNetCore.Components.Authorization;

namespace ClientApp.UIShared;

public sealed partial class MainLayout
{
    private readonly MudTheme _theme = new();
    private bool _drawerOpen = true;
    private bool _settingsOpen = false;
    private SettingsPanel? _settingsPanel;
    private bool _isLoadingPromptsInit;

    private Task _copilotPromptsInitializing;

    private bool _isDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkTheme);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkTheme, value);
    }

    private bool _isReversed
    {
        get => LocalStorage.GetItem<bool?>(StorageKeys.PrefersReversedConversationSorting) ?? false;
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersReversedConversationSorting, value);
    }

    private bool _isRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }
    [Inject] public required ApiClient ApiClient { get; set; }
    [Inject] public required AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    private bool SettingsDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "ask" or "chat" => false,
        _ => true
    };

    private bool SortDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "voicechat" or "chat" => false,
        _ => true
    };

    public CopilotPromptsRequestResponse CopilotPrompts { get; set; } = new();

    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;

    private void OnThemeChanged() => _isDarkTheme = !_isDarkTheme;

    private void OnIsReversedChanged() => _isReversed = !_isReversed;

    protected override async Task OnInitializedAsync()
    {
        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _copilotPromptsInitializing = OnCopilotPromptsInitializingAsync();
    }

    

    private async Task OnUpdateButtonClickedAsync()
    {
        await ApiClient.PostCopilotPromptsServerDataAsync(CopilotPrompts);
    }

    private async Task OnCopilotPromptsInitializingAsync()
    {
        _isLoadingPromptsInit = true;

        try
        {
            CopilotPrompts = await ApiClient.GetCopilotPromptsAsync();
        }
        finally
        {
            _isLoadingPromptsInit = false;
            StateHasChanged();
        }
    }
}
