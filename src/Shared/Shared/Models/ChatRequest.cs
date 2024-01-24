namespace Shared.Models;

public class ChatRequest(ChatTurn[] history, Approach approach, SearchParameters overrides = null) : ApproachRequest(approach)
{
    public ChatTurn[] History { get; set; } = history;
    public SearchParameters Overrides { get; set; } = overrides;

    public string LastUserQuestion => History?.LastOrDefault()?.User;
}

