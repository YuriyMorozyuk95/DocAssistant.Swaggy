namespace ClientApp.Components;

public sealed partial class UserGroupsMultiSelect
{
    [Inject]
    private ILogger<UserGroupsMultiSelect> Logger { get; set; }
    [Inject]
    public IPermissionApiClient UserGroupApiClient { get; set; }
    [Parameter]
    public IEnumerable<UserGroup> SelectedItems { get; set; } = new List<UserGroup>();
    [Parameter]
    public EventCallback<IEnumerable<UserGroup>> SelectedItemsChanged { get; set; }
    [Parameter]
    public string Label { get; set; }
    [Parameter]
    public bool IsEnabled { get; set; }

    private IEnumerable<UserGroup> _items = new List<UserGroup>();
    private bool _isInitialized;
    protected override async Task OnInitializedAsync()
    {

        _items = (await UserGroupApiClient.GetUserGroups()).ToList();
        _isInitialized = true;
        StateHasChanged();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
    }

    private async Task OnSelectedItemsChangedAsync(IEnumerable<UserGroup> arg)
    {
        if (_isInitialized)
        {
            var argsIds = arg.Select(x => x.Id).ToList();
            var selectedItemsFromItems = _items.Where(x => argsIds.Contains(x.Id)).ToList();

            SelectedItems = selectedItemsFromItems;
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
            StateHasChanged();
        }
    }
}
