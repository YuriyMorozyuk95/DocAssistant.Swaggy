namespace Shared;

public class IndexCreationInfo
{
	public IndexStatus LastIndexStatus { get; set; } = IndexStatus.NotStarted;
    public string LastIndexErrorMessage { get; set; }

    public string StepInfo { get; set; }
    public int Value { get; set; }
    public int ValueBuffer => Value + 2;
    public int Max { get; set; }
}

public enum IndexStatus
{
    Processing = 0,
    Succeeded = 1,
    Failed = 2,
    NotStarted = 3,
}

