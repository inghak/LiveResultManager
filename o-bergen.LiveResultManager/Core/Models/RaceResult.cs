namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Core domain model matching results.json structure
/// </summary>
public class RaceResult
{
    public string Id { get; set; } = string.Empty;
    public string ECard { get; set; } = string.Empty;
    public string? ECard2 { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? Time { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public string Class { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public string? TeamName { get; set; }
    public List<SplitTime> SplitTimes { get; set; } = new();
}

/// <summary>
/// Split time matching results.json structure
/// </summary>
public class SplitTime
{
    public int Number { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Splittime { get; set; }
    public int Totaltime { get; set; }
}
