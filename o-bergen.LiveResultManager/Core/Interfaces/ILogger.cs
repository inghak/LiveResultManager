namespace o_bergen.LiveResultManager.Core.Interfaces;

/// <summary>
/// Custom logging interface for application logging
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    void LogInformation(string message);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message
    /// </summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Logs a debug message
    /// </summary>
    void LogDebug(string message);

    /// <summary>
    /// Logs a success message
    /// </summary>
    void LogSuccess(string message);
}
