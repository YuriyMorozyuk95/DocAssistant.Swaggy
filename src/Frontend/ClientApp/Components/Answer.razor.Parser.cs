namespace ClientApp.Components;

public sealed partial class Answer
{
    internal static HtmlParsedAnswer ParseAnswerToHtml(string answer, string citationBaseUrl, Shared.Models.SupportingContent[] supportingContentRecords)
    {
        var citations = new List<CitationDetails>();
        var followupQuestions = new HashSet<string>();

        var parsedAnswer = ReplacementRegex().Replace(answer, match =>
        {
            followupQuestions.Add(match.Value);
            return "";
        });

        parsedAnswer = parsedAnswer.Trim();

        var parts = SplitRegex().Split(parsedAnswer);

        var fragments = parts.Select((part, index) =>
        {
            if (index % 2 is 0)
            {
                return part;
            }
            else
            {
                var citationNumber = citations.Count + 1;
                var existingCitation = citations.FirstOrDefault(c => c.Name == part);
                if (existingCitation is not null)
                {
                    citationNumber = existingCitation.Number;
                }
                else
                {
                    var citation = new CitationDetails(part, citationBaseUrl, citationNumber);
                    citations.Add(citation);
                }

                return $"""
                    <sup class="mud-chip mud-chip-text mud-chip-color-info rounded pa-1">{citationNumber}</sup>
                    """;
            }
        }).ToArray();

        foreach (var citation in citations)
        {
            var originUri = supportingContentRecords.FirstOrDefault(s => string.Equals(s.Title, citation.Name, StringComparison.InvariantCultureIgnoreCase))?.OriginUri;
            citation.OriginUri = originUri;
        }

        return new HtmlParsedAnswer(
            string.Join("", fragments),
            citations,
            followupQuestions.Select(f => f.Replace("<<", "").Replace(">>", ""))
                .ToHashSet());
    }

    [GeneratedRegex(@"<<([^>>]+)>>", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex ReplacementRegex();

    [GeneratedRegex(@"\[([^\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex SplitRegex();
}

internal class HtmlParsedAnswer  
{  
    public HtmlParsedAnswer(string answerHtml, List<CitationDetails> citations, HashSet<string> followupQuestions)  
    {  
        this.AnswerHtml = answerHtml;  
        this.Citations = citations;  
        this.FollowupQuestions = followupQuestions;  
    }  
  
    public string AnswerHtml { get; set; }  
  
    public List<CitationDetails> Citations { get; set; }  
  
    public HashSet<string> FollowupQuestions { get; set; }  
}  

