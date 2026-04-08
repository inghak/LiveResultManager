using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Core.Interfaces;

/// <summary>
/// Represents a destination for race results (Supabase, SQL Server, API, etc.)
/// Implements the Adapter pattern for destination abstraction
/// </summary>
public interface IResultDestination
{
    /// <summary>
    /// Gets the name/type of the destination
    /// </summary>
    string DestinationName { get; }

    /// <summary>
    /// Writes/upserts results to the destination
    /// </summary>
    /// <param name="results">Results to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records successfully written</returns>
    Task<int> WriteResultsAsync(IReadOnlyList<RaceResult> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current record count in destination
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records in destination</returns>
    Task<int> GetRecordCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if the destination is accessible and can be written to
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes results by ID from the destination
    /// </summary>
    /// <param name="ids">IDs of results to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records successfully deleted</returns>
    Task<int> DeleteResultsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default);
}
