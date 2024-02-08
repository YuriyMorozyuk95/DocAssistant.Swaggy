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

    private void OnShowCitation(CitationDetails citation) => Dialog.Show<PdfViewerDialog>(
            $"📄 {citation.Name}",
            new DialogParameters
            {
                [nameof(PdfViewerDialog.FileName)] = citation.Name,
                [nameof(PdfViewerDialog.BaseUrl)] = citation.BaseUrl,
                [nameof(PdfViewerDialog.OriginUri)] = citation.OriginUri,
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

    private async Task ShowMergedSwaggerDocument() => await Dialog.ShowAsync<PdfViewerDialog>(
        $"📄 Merged swagger file that was used for creating curl for endpoint : {Retort.SwaggerDocument.Endpoints}",
        new DialogParameters
        {
            [nameof(PdfViewerDialog.JsonContent)] = Retort.SwaggerDocument.SwaggerContent,
        },
        new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        });
}
