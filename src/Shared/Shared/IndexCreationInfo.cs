namespace Shared;

public class IndexCreationInfo
{
	public IndexStatus LastIndexStatus { get; set; } = IndexStatus.NotStarted;
    public string LastIndexErrorMessage { get; set; }
    public int ChunksProcessed { get; set; }
    public int DocumentPageProcessed { get; set; }
    public int DocumentPageProcessedBuffer => DocumentPageProcessed + 2;
    public int TotalPageCount { get; set; }
}

public enum IndexStatus
{
    Processing = 0,
    Succeeded = 1,
    Failed = 2,
    NotStarted = 3,
}

