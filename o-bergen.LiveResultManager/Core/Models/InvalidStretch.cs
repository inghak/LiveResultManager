namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Represents an invalid stretch between two controls for a specific event
/// </summary>
public class InvalidStretch
{
    /// <summary>
    /// Unique identifier for the stretch
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event identifier (Name + Date, e.g., "VBIK Orientering 2026_2026-04-08")
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Event name for display purposes
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Event date for display purposes
    /// </summary>
    public string EventDate { get; set; } = string.Empty;

    /// <summary>
    /// First control code in the stretch
    /// </summary>
    public string FromControlCode { get; set; } = string.Empty;

    /// <summary>
    /// Second control code in the stretch
    /// </summary>
    public string ToControlCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional description/reason for the invalid stretch
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this stretch was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Creates an EventId from name and date
    /// </summary>
    public static string CreateEventId(string eventName, string eventDate)
    {
        return $"{eventName}_{eventDate}";
    }

    /// <summary>
    /// Display string for the stretch
    /// </summary>
    public override string ToString()
    {
        return $"{FromControlCode} → {ToControlCode}" + 
               (string.IsNullOrWhiteSpace(Description) ? "" : $" ({Description})");
    }
}
