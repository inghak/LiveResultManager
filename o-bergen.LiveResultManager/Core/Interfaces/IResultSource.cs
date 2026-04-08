using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Core.Interfaces;

/// <summary>
/// Represents a source of race results (Access DB, SQL Server, API, etc.)
/// Implements the Repository pattern for data access abstraction
/// </summary>
public interface IResultSource
{
    /// <summary>
    /// Gets the name/type of the source
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Reads all results from the source
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Read-only list of race results</returns>
    Task<IReadOnlyList<RaceResult>> ReadResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last modified timestamp of the source data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Last modified timestamp, or null if not available</returns>
    Task<DateTime?> GetLastModifiedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if the source is accessible and can be read from
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
