namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Lightweight participant record used for duplicate detection
/// </summary>
public record ParticipantEntry(
    string Id,
    string FirstName,
    string LastName,
    string ECard,
    string Class);
