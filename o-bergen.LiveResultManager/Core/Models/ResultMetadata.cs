namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Metadata about a transfer operation
/// </summary>
public class ResultMetadata
{
    /// <summary>
    /// Event metadata from source database
    /// </summary>
    public EventMetadata? EventMetadata { get; set; }

    /// <summary>
    /// Timestamp when the transfer was executed
    /// </summary>
    public DateTime TransferDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Name of the source system
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the destination system
    /// </summary>
    public string DestinationName { get; set; } = string.Empty;

    /// <summary>
    /// Number of records read from source
    /// </summary>
    public int RecordsRead { get; set; }

    /// <summary>
    /// Number of records successfully written to destination
    /// </summary>
    public int RecordsWritten { get; set; }

    /// <summary>
    /// Number of records deleted from destination (delta detection)
    /// </summary>
    public int RecordsDeleted { get; set; }

    /// <summary>
    /// Number of records adjusted for invalid stretches
    /// </summary>
    public int RecordsAdjustedForInvalidStretch { get; set; }

    /// <summary>
    /// Indicates if the transfer was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if transfer failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the transfer operation
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Archive path where results were saved
    /// </summary>
    public string? ArchivePath { get; set; }
}
