using Shared.Models.Swagger;

using System.Threading;
using Microsoft.CognitiveServices.Speech;
using Microsoft.JSInterop;

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;

    private string _firstExample;
    private string _secondExample;
    private string _thirdExample;

    private readonly Dictionary<UserQuestion, SwaggerCompletionInfo> _questionAndAnswerMap = new();
    private bool _isLoadingPromptsInit;
    private bool _isExamplesPromptsInit;
    private Task _copilotPromptsInitializing;
    private Task _examplesPromptsInitialing;

    [Inject] public required ISessionStorageService SessionStorage { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }

    [Inject]
    public required IJSRuntime JsRuntime { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; } = new RequestSettingsOverrides();

    [CascadingParameter(Name = nameof(CopilotPrompts))]
    public required CopilotPromptsRequestResponse CopilotPrompts { get; set; } = new();  

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        if (string.IsNullOrWhiteSpace(_userQuestion))
        {
            return;
        }

        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _currentQuestion = new(_userQuestion, DateTime.Now);
        _questionAndAnswerMap[_currentQuestion] = null;

        try
        {
            var history = _questionAndAnswerMap
                .Where(x => x.Value is not null)
                .Select(x => new ChatTurn(x.Key.Question, x.Value!.FinalResult))
                .ToList();

            history.Add(new ChatTurn(_userQuestion));

            var request = new ChatRequest(history.ToArray(), Settings.Approach, Settings.Overrides);
            var result = await ApiClient.ChatToApiConversationAsync(request);

            _questionAndAnswerMap[_currentQuestion] = result;
            if (result.IsSuccessful)
            {
                _userQuestion = "";
                _currentQuestion = default;
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }

    private void OnClearChat()
    {
        _userQuestion = _lastReferenceQuestion = "";
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
    }

    private async Task OnVoiceClicked(SwaggerCompletionInfo answer)
    {
        if(answer == null)
        {
            return;
        }
        string text = answer.FinalResult;

        var audioData = await ApiClient.PostTextToSpeech(text, CancellationToken.None);
        await JsRuntime.InvokeVoidAsync("playAudioData", audioData);
    }
}
