namespace Shared.Models;

public class ApproachResponse
{
	public ApproachResponse(string error)
	{
		Error = error;
	}

	public ApproachResponse(string answer, string thoughts, SupportingContent[] dataPoints, string citationBaseUrl, string[] questions, string error = null)
	{
		Answer = answer;
		Thoughts = thoughts;
		DataPoints = dataPoints;
		CitationBaseUrl = citationBaseUrl;
		Questions = questions;
		Error = error;
	}

	public string Answer { get; set; }
	public string Thoughts { get; set; }
	public SupportingContent[] DataPoints { get; set; }
	public string CitationBaseUrl { get; set; }
	public string[] Questions { get; set; }
	public string Error { get; set; }
}