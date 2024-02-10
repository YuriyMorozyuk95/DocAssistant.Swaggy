//using IDialogService = Microsoft.FluentUI.AspNetCore.Components.IDialogService;
//TODO

using Shared.Models.Swagger;

using DialogParameters = MudBlazor.DialogParameters;
using IDialogService = MudBlazor.IDialogService;

namespace ClientApp.Components;

public sealed partial class Answer
{
    [Parameter, EditorRequired] public required SwaggerCompletionInfo Retort { get; set; }
    [Parameter, EditorRequired] public required EventCallback<string> FollowupQuestionClicked { get; set; }

    [Inject] public required IDialogService Dialog { get; set; }

    private async Task OnAskFollowupAsync(string followupQuestion)
    {
        if (FollowupQuestionClicked.HasDelegate)
        {
            await FollowupQuestionClicked.InvokeAsync(followupQuestion);
        }
    }

    private void OnShowCitation(CitationDetails citation) => Dialog.Show<JsonViewerDialog>(
            $"📄 {citation.Name}",
            new DialogParameters
            {
                [nameof(JsonViewerDialog.FileName)] = citation.Name,
                [nameof(JsonViewerDialog.BaseUrl)] = citation.BaseUrl,
                [nameof(JsonViewerDialog.OriginUri)] = citation.OriginUri,
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });

    private MarkupString RemoveLeadingAndTrailingLineBreaks(string input) => (MarkupString)HtmlLineBreakRegex().Replace(input, "");

    [GeneratedRegex("^(\\s*<br\\s*/?>\\s*)+|(\\s*<br\\s*/?>\\s*)+$", RegexOptions.Multiline)]
    private static partial Regex HtmlLineBreakRegex();

    private async Task ShowMergedSwaggerDocument() => await Dialog.ShowAsync<JsonViewerDialog>(
        $"📄 Merged swagger file with best matched endpoints",
        new DialogParameters
        {
            [nameof(JsonViewerDialog.JsonContent)] = Retort.SwaggerDocument.SwaggerContent,
            [nameof(JsonViewerDialog.BaseUrl)] = Retort.SwaggerDocument.SwaggerContentUrl,
        },
        new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        });
}
