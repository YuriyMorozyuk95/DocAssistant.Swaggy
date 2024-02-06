using IDialogService = MudBlazor.IDialogService;

using Color = MudBlazor.Color;

namespace ClientApp.UIShared;

public sealed partial class MainLayout
{
    private readonly MudTheme _theme = new();

    public MainLayout()
    {
        _theme.Palette.Primary = _theme.Palette.Info;
        _theme.PaletteDark.Primary = _theme.Palette.Info;
        _theme.Typography = new MudBlazor.Typography()
        {
            Default = new Default()
            {
                FontFamily = new[] { "Segoe UI", "-apple-system", "BlinkMacSystemFont","Roboto", "sans-serif" }
            }
        };
    }

    private bool _drawerOpen = true;
    private bool _settingsOpen = false;

    private bool _isDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkTheme);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkTheme, value);
    }

    public Color Color => _isDarkTheme ? Color.Dark : Color.Info;

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

    private bool SettingsDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "ask" or "chat" => true,
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
}
