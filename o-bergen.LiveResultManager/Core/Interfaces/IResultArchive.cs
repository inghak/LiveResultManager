using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Core.Interfaces;

/// <summary>
/// Represents an archive for race results (file system, blob storage, etc.)
/// </summary>
public interface IResultArchive
{
    /// <summary>
    /// Archives results with metadata to persistent storage
    /// </summary>
    /// <param name="results">Results to archive</param>
    /// <param name="metadata">Metadata about the transfer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to archived results</returns>
    Task<string> ArchiveResultsAsync(
        IReadOnlyList<RaceResult> results,
        ResultMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves archived results by date
    /// </summary>
    /// <param name="date">Date to retrieve results for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of results and metadata, or null if not found</returns>
    Task<(IReadOnlyList<RaceResult> results, ResultMetadata metadata)?> GetArchivedResultsAsync(
        DateTime date,
        CancellationToken cancellationToken = default);
}
