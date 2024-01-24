namespace ClientApp.Components;

public sealed partial class Examples
{
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }

    [Parameter] public EventCallback<string> FirstExampleChanged { get; set; }
    [Parameter] public EventCallback<string> SecondExampleChanged { get; set; }
    [Parameter] public EventCallback<string> ThirdExampleChanged { get; set; }

    [Parameter]
    public string FirstExample { get; set; } = "Loading...";

    [Parameter]
    public string SecondExample { get; set; } = "Loading...";

    [Parameter]
    public string ThirdExample { get; set; } = "Loading...";

    private async Task OnClickedAsync(string exampleText)
    {
        if (OnExampleClicked.HasDelegate)
        {
            await OnExampleClicked.InvokeAsync(exampleText);
        }
    }
}
