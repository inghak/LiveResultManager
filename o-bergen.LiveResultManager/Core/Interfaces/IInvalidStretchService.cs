using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Core.Interfaces;

/// <summary>
/// Service for managing invalid stretches and calculating time adjustments
/// </summary>
public interface IInvalidStretchService
{
    /// <summary>
    /// Get all invalid stretches for a specific event
    /// </summary>
    List<InvalidStretch> GetStretchesForEvent(string eventId);

    /// <summary>
    /// Get all invalid stretches
    /// </summary>
    List<InvalidStretch> GetAllStretches();

    /// <summary>
    /// Add a new invalid stretch
    /// </summary>
    void AddStretch(InvalidStretch stretch);

    /// <summary>
    /// Remove an invalid stretch by ID
    /// </summary>
    bool RemoveStretch(string stretchId);

    /// <summary>
    /// Update an existing stretch
    /// </summary>
    bool UpdateStretch(InvalidStretch stretch);

    /// <summary>
    /// Calculate time adjustment for a result based on invalid stretches
    /// Returns the number of seconds to deduct from the total time
    /// </summary>
    int CalculateTimeAdjustment(RaceResult result, string eventId);

    /// <summary>
    /// Get description of adjustments made (for status message)
    /// </summary>
    string GetAdjustmentDescription(RaceResult result, string eventId);

    /// <summary>
    /// Save configuration to disk
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Reload configuration from disk
    /// </summary>
    Task ReloadAsync();
}
