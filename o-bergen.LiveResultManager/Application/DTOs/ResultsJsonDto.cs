namespace o_bergen.LiveResultManager.Application.DTOs;

/// <summary>
/// Root DTO for results.json file
/// Represents the translation layer between source and destination
/// </summary>
public record ResultsJsonDto
{
    /// <summary>
    /// Schema version for future compatibility
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Timestamp when this file was generated
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// List of race results
    /// </summary>
    public List<ResultDto> Results { get; init; } = new();

    /// <summary>
    /// Total number of results in this file
    /// </summary>
    public int TotalResults => Results.Count;
}
