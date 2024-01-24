
using Microsoft.FluentUI.AspNetCore.Components;

namespace ClientApp.Components;

//TODO part of migration to FluentUI
public sealed partial class FluentPermissionMultiSelect
{
    [Inject]
    private ILogger<UserGroupsMultiSelect> Logger { get; set; }
    [Inject]
    public IPermissionApiClient UserGroupApiClient { get; set; }

    [Parameter]
    public IEnumerable<UserGroup> SelectedItems
    {
        get => _selectedItems;
        set
        {
            _selectedItems = value;
            //OnSelectedItemsChanged(_selectedItems);
        }
    }

    [Parameter]  
    public EventCallback<IEnumerable<UserGroup>> SelectedItemsChanged { get; set; }
    [Parameter]  
    public string Label { get; set; }
    [Parameter]  
    public bool IsEnabled { get; set; }  

    //public Type PermissionEntityType { get; set; } = typeof(PermissionEntity);
    private IEnumerable<UserGroup> _items /*= new List<PermissionEntity>()*/;
    private bool _isInitialized;
    private IEnumerable<UserGroup> _selectedItems /*= new List<PermissionEntity>()*/;

    protected override async Task OnInitializedAsync()
    {

        _items = (await UserGroupApiClient.GetUserGroups()).ToList();
        _isInitialized = true;
        StateHasChanged();
    }

    protected override Task OnParametersSetAsync()
    {
        return base.OnParametersSetAsync();
    }

    private Task OnSearchAsync(OptionsSearchEventArgs<UserGroup> arg)
    {
        arg.Items = _items.Where(i => i.Name.StartsWith(arg.Text, StringComparison.OrdinalIgnoreCase))  
            .OrderBy(i => i.Name);

        return Task.CompletedTask;
    }

    private void OnSelectedItemsChanged(IEnumerable<UserGroup> arg)
    {
        if (_isInitialized)
        {
            var argsIds = arg.Select(x => x.Id).ToList();
            var selectedItemsFromItems = _items.Where(x => argsIds.Contains(x.Id)).ToList();

            SelectedItems = selectedItemsFromItems;  
            //await SelectedItemsChanged.InvokeAsync(SelectedItems);
            StateHasChanged();
        }
    }
}
