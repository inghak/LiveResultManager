namespace o_bergen.LiveResultManager.Application.DTOs;

/// <summary>
/// DTO for metadata.json file
/// Contains information about the transfer operation AND event metadata
/// </summary>
public record MetadataJsonDto
{
    /// <summary>
    /// Event metadata
    /// </summary>
    public EventMetadataDto? Event { get; init; }

    /// <summary>
    /// Timestamp when the transfer was executed
    /// </summary>
    public DateTime TransferDate { get; init; }

    /// <summary>
    /// Name of the source system
    /// </summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>
    /// Name of the destination system
    /// </summary>
    public string DestinationName { get; init; } = string.Empty;

    /// <summary>
    /// Number of records read from source
    /// </summary>
    public int RecordsRead { get; init; }

    /// <summary>
    /// Number of records successfully written to destination
    /// </summary>
    public int RecordsWritten { get; init; }

    /// <summary>
    /// Number of records deleted from destination (delta detection)
    /// </summary>
    public int RecordsDeleted { get; init; }

    /// <summary>
    /// Indicates if the transfer was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if transfer failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Duration of the transfer in seconds
    /// </summary>
    public double? DurationSeconds { get; init; }

    /// <summary>
    /// Archive path where results were saved
    /// </summary>
    public string? ArchivePath { get; init; }
}

/// <summary>
/// Event metadata DTO
/// </summary>
public record EventMetadataDto
{
    public string Name { get; init; } = "Bedriftsløp";
    public string Date { get; init; } = string.Empty;
    public string Organizer { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public List<CourseDto> Courses { get; init; } = new();
    public string EventType { get; init; } = "Normal";
    public string TerrainType { get; init; } = "Skog";
}

/// <summary>
/// Course DTO
/// </summary>
public record CourseDto
{
    public string Name { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public double Length { get; init; }
    public List<ControlDto> Controls { get; init; } = new();
}

/// <summary>
/// Control DTO
/// </summary>
public record ControlDto
{
    public int No { get; init; }
    public string Code { get; init; } = string.Empty;
}
