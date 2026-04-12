namespace o_bergen.LiveResultManager.Application.DTOs;

/// <summary>
/// Data Transfer Object for a single race result
/// Used in results.json file - matches exact format from BackgroundSource
/// </summary>
public record ResultDto
{
    public string? Id { get; init; }
    public string? ECard { get; init; }
    public string? ECard2 { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? StartTime { get; init; }
    public string? Time { get; init; }
    public string? Status { get; init; }
    public string? StatusMessage { get; init; }
    public string? Class { get; init; }
    public string? Course { get; init; }
    public string? Points { get; init; }
    public string? TeamId { get; init; }
    public string? TeamName { get; init; }
    public List<SplitTimeDto>? SplitTimes { get; init; }
}

/// <summary>
/// Split time DTO for JSON serialization
/// Matches exact format from BackgroundSource
/// </summary>
public record SplitTimeDto
{
    public int Number { get; init; }
    public string? Code { get; init; }
    public int Splittime { get; init; }
    public int Totaltime { get; init; }
}
