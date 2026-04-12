using System.Diagnostics;
using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Core.Services;

/// <summary>
/// Core orchestration service for transferring race results
/// Coordinates reading from source, archiving, and writing to destination
/// </summary>
public class ResultTransferService
{
    private readonly IResultSource _source;
    private readonly IResultDestination _destination;
    private readonly IResultArchive _archive;
    private readonly IInvalidStretchService? _invalidStretchService;
    private readonly ILogger? _logger;
    private Dictionary<string, RaceResult> _previousResults = new();

    /// <summary>
    /// Event raised when a log message is generated
    /// </summary>
    public event EventHandler<string>? LogMessage;

    /// <summary>
    /// Event raised when transfer status changes
    /// </summary>
    public event EventHandler<TransferStatus>? StatusChanged;

    /// <summary>
    /// Event raised with progress information
    /// </summary>
    public event EventHandler<TransferProgressEventArgs>? ProgressChanged;

    public ResultTransferService(
        IResultSource source,
        IResultDestination destination,
        IResultArchive archive,
        IInvalidStretchService? invalidStretchService = null,
        ILogger? logger = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _invalidStretchService = invalidStretchService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a complete transfer operation
    /// </summary>
    public async Task<ResultMetadata> ExecuteTransferAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metadata = new ResultMetadata
        {
            TransferDate = DateTime.Now,
            SourceName = _source.SourceName,
            DestinationName = _destination.DestinationName
        };

        try
        {
            Log($"🚀 Starting transfer from {_source.SourceName} to {_destination.DestinationName}", LogLevel.Information);
            NotifyStatus(TransferStatus.Running);
            NotifyProgress("Initializing transfer...", 0);

            // Step 1: Fetch event metadata (if source is AccessDb)
            if (_source is Infrastructure.Sources.AccessDbResultSource accessDbSource)
            {
                Log("📋 Fetching event metadata...", LogLevel.Information);
                var eventMetadata = await accessDbSource.FetchMetadataAsync(cancellationToken);
                metadata.EventMetadata = eventMetadata;
                Log($"✅ Event metadata fetched: {eventMetadata.Name} at {eventMetadata.Location}", LogLevel.Success);

                // Upload metadata to Supabase if destination supports it
                if (_destination is Infrastructure.Destinations.SupabaseResultDestination supabaseDestination)
                {
                    Log("📤 Uploading metadata to Supabase...", LogLevel.Information);
                    await supabaseDestination.UploadMetadataAsync(eventMetadata);
                    Log("✅ Metadata uploaded to live_competitions", LogLevel.Success);
                }
            }

            // Step 2: Read from source
            Log("📖 Reading from source...", LogLevel.Information);
            NotifyProgress("Reading from source...", 10);
            var results = await _source.ReadResultsAsync(cancellationToken);
            metadata.RecordsRead = results.Count;
            Log($"✅ Read {results.Count} records from {_source.SourceName}", LogLevel.Success);
            NotifyProgress($"Read {results.Count} records", 30);

            // Step 2.5: Detect and delete removed results (delta detection)
            var currentResultsById = results.ToDictionary(r => r.Id);
            var deletedIds = _previousResults.Keys.Except(currentResultsById.Keys).ToList();

            if (deletedIds.Count > 0)
            {
                Log($"🗑️ Detected {deletedIds.Count} removed results, deleting from destination...", LogLevel.Information);
                NotifyProgress($"Deleting {deletedIds.Count} removed results...", 35);

                try
                {
                    var deletedCount = await _destination.DeleteResultsAsync(deletedIds, cancellationToken);
                    metadata.RecordsDeleted = deletedCount;
                    Log($"✅ Deleted {deletedCount} results from {_destination.DestinationName}", LogLevel.Success);
                }
                catch (Exception ex)
                {
                    Log($"⚠️ Warning: Failed to delete removed results: {ex.Message}", LogLevel.Warning);
                }
            }

            // Update snapshot for next transfer
            _previousResults = currentResultsById;

            if (results.Count == 0)
            {
                Log("⚠️ No results to transfer", LogLevel.Warning);
                metadata.Success = true;
                metadata.Duration = stopwatch.Elapsed;
                NotifyStatus(TransferStatus.Success);
                return metadata;
            }

            // Step 3: Archive (includes results.json + metadata.json)
            Log("💾 Archiving results...", LogLevel.Information);
            NotifyProgress("Archiving results...", 50);

            // Create preliminary metadata for archiving (will be updated with final values)
            var archiveMetadata = new ResultMetadata
            {
                EventMetadata = metadata.EventMetadata,
                TransferDate = metadata.TransferDate,
                SourceName = metadata.SourceName,
                DestinationName = metadata.DestinationName,
                RecordsRead = metadata.RecordsRead,
                Success = false // Will be updated after destination write
            };

            var archivePath = await _archive.ArchiveResultsAsync(results, archiveMetadata, cancellationToken);
            metadata.ArchivePath = archivePath;
            Log($"✅ Archived to {archivePath}", LogLevel.Success);
            Log("  📄 results.json (backup)", LogLevel.Information);
            Log("  📄 metadata.json (backup)", LogLevel.Information);
            Log("  📄 iofres.xml (IOF XML 3.0)", LogLevel.Success);
            Log("  📄 Sploype.csv (World of O)", LogLevel.Success);

            // Check if files were actually created to verify Supabase upload occurred
            if (Directory.Exists(archivePath))
            {
                var iofXmlPath = Path.Combine(archivePath, "iofres.xml");
                var sploypePath = Path.Combine(archivePath, "Sploype.csv");

                if (File.Exists(iofXmlPath) && File.Exists(sploypePath))
                {
                    Log("  ☁️ Files also uploaded to Supabase Storage (if ServiceRoleKey configured)", LogLevel.Information);
                }
            }

            NotifyProgress("Archive complete", 70);

            // Step 4: Apply invalid stretch adjustments
            if (_invalidStretchService != null && metadata.EventMetadata != null)
            {
                var eventId = InvalidStretch.CreateEventId(metadata.EventMetadata.Name, metadata.EventMetadata.Date);
                var stretches = _invalidStretchService.GetStretchesForEvent(eventId);

                if (stretches.Count > 0)
                {
                    Log($"⚠️ Applying {stretches.Count} invalid stretch adjustment(s)...", LogLevel.Information);
                    Log($"   Event ID: {eventId}", LogLevel.Information);

                    foreach (var stretch in stretches)
                    {
                        Log($"   Stretch: {stretch.FromControlCode} → {stretch.ToControlCode}", LogLevel.Information);
                    }

                    int adjustedCount = 0;
                    int checkedCount = 0;

                    foreach (var result in results)
                    {
                        // Log first 3 results' split times for debugging
                        if (checkedCount < 3 && result.SplitTimes?.Count > 0)
                        {
                            var codes = string.Join(", ", result.SplitTimes.Select(st => st.Code));
                            Log($"   Sample result {result.Id} controls: {codes}", LogLevel.Information);
                        }
                        checkedCount++;

                        var adjustment = _invalidStretchService.CalculateTimeAdjustment(result, eventId);
                        if (adjustment > 0)
                        {
                            var description = _invalidStretchService.GetAdjustmentDescription(result, eventId);

                            // Adjust the time - handle both "1470" (seconds) and "24:30" (MM:SS) formats
                            if (!string.IsNullOrEmpty(result.Time))
                            {
                                int originalSeconds;
                                bool isTimeFormat = result.Time.Contains(':');

                                if (isTimeFormat)
                                {
                                    // Parse MM:SS format
                                    var parts = result.Time.Split(':');
                                    if (parts.Length == 2 && int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds))
                                    {
                                        originalSeconds = minutes * 60 + seconds;
                                    }
                                    else
                                    {
                                        continue; // Skip if format is invalid
                                    }
                                }
                                else if (int.TryParse(result.Time, out originalSeconds))
                                {
                                    // Already in seconds
                                }
                                else
                                {
                                    continue; // Skip if format is invalid
                                }

                                int adjustedSeconds = Math.Max(0, originalSeconds - adjustment);

                                // Convert back to original format
                                if (isTimeFormat)
                                {
                                    int adjMinutes = adjustedSeconds / 60;
                                    int adjSecs = adjustedSeconds % 60;
                                    result.Time = $"{adjMinutes}:{adjSecs:D2}";
                                }
                                else
                                {
                                    result.Time = adjustedSeconds.ToString();
                                }

                                Log($"   ✓ Adjusting {result.FirstName} {result.LastName} (ID: {result.Id}): {originalSeconds}s → {adjustedSeconds}s (-{adjustment}s)", LogLevel.Success);

                                // Append adjustment info to status message
                                if (!string.IsNullOrEmpty(description))
                                {
                                    result.StatusMessage = string.IsNullOrEmpty(result.StatusMessage)
                                        ? description
                                        : $"{result.StatusMessage}; {description}";
                                }

                                adjustedCount++;
                            }
                        }
                    }

                    if (adjustedCount > 0)
                    {
                        Log($"✅ Adjusted times for {adjustedCount} result(s)", LogLevel.Success);
                    }
                    else
                    {
                        Log($"⚠️ No results matched the configured stretches (checked {checkedCount} results)", LogLevel.Warning);
                    }
                }
            }

            // Step 5: Write to destination
            Log("📤 Writing to destination...", LogLevel.Information);
            NotifyProgress("Writing to destination...", 80);
            var written = await _destination.WriteResultsAsync(results, cancellationToken);
            metadata.RecordsWritten = written;
            Log($"✅ Wrote {written} records to {_destination.DestinationName}", LogLevel.Success);
            NotifyProgress($"Wrote {written} records", 90);

            // Mark as successful
            metadata.Success = true;
            metadata.Duration = stopwatch.Elapsed;

            Log($"🎉 Transfer completed successfully in {metadata.Duration.Value.TotalSeconds:F1}s", LogLevel.Success);
            NotifyStatus(TransferStatus.Success);
            NotifyProgress("Transfer complete", 100);

            // Update archived metadata with final success status
            await UpdateArchivedMetadataAsync(results, metadata, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            metadata.Success = false;
            metadata.ErrorMessage = "Transfer was cancelled";
            metadata.Duration = stopwatch.Elapsed;
            Log("⚠️ Transfer cancelled by user", LogLevel.Warning);
            NotifyStatus(TransferStatus.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            metadata.Success = false;
            metadata.ErrorMessage = ex.Message;
            metadata.Duration = stopwatch.Elapsed;

            Log($"❌ ERROR: {ex.Message}", LogLevel.Error);
            _logger?.LogError($"Transfer failed: {ex.Message}", ex);
            NotifyStatus(TransferStatus.Error);

            throw;
        }

        return metadata;
    }

    /// <summary>
    /// Tests connections to both source and destination
    /// </summary>
    public async Task<(bool sourceOk, bool destinationOk)> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        Log("🔌 Testing connections...", LogLevel.Information);

        var sourceOk = await _source.TestConnectionAsync(cancellationToken);
        Log(sourceOk 
            ? $"✅ Source ({_source.SourceName}) connection OK" 
            : $"❌ Source ({_source.SourceName}) connection FAILED", 
            sourceOk ? LogLevel.Success : LogLevel.Error);

        var destinationOk = await _destination.TestConnectionAsync(cancellationToken);
        Log(destinationOk 
            ? $"✅ Destination ({_destination.DestinationName}) connection OK" 
            : $"❌ Destination ({_destination.DestinationName}) connection FAILED",
            destinationOk ? LogLevel.Success : LogLevel.Error);

        return (sourceOk, destinationOk);
    }

    /// <summary>
    /// Gets the last modified timestamp from the source
    /// </summary>
    public async Task<DateTime?> GetSourceLastModifiedAsync(CancellationToken cancellationToken = default)
    {
        return await _source.GetLastModifiedAsync(cancellationToken);
    }

    private async Task UpdateArchivedMetadataAsync(
        IReadOnlyList<RaceResult> results, 
        ResultMetadata metadata, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Re-archive with updated metadata (success status)
            await _archive.ArchiveResultsAsync(results, metadata, cancellationToken);
        }
        catch (Exception ex)
        {
            Log($"⚠️ Warning: Could not update archived metadata: {ex.Message}", LogLevel.Warning);
        }
    }

    private void Log(string message, LogLevel level)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = level switch
        {
            LogLevel.Success => "SUCCESS",
            LogLevel.Error => "ERROR",
            LogLevel.Warning => "WARNING",
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DEBUG",
            _ => "LOG"
        };

        var formattedMessage = $"[{timestamp}] [{prefix}] {message}";
        
        LogMessage?.Invoke(this, formattedMessage);
        
        // Also log to custom logger if provided
        switch (level)
        {
            case LogLevel.Success:
                _logger?.LogSuccess(message);
                break;
            case LogLevel.Error:
                _logger?.LogError(message);
                break;
            case LogLevel.Warning:
                _logger?.LogWarning(message);
                break;
            case LogLevel.Information:
                _logger?.LogInformation(message);
                break;
            case LogLevel.Debug:
                _logger?.LogDebug(message);
                break;
        }
    }

    private void NotifyStatus(TransferStatus status)
    {
        StatusChanged?.Invoke(this, status);
    }

    private void NotifyProgress(string message, int percentage)
    {
        ProgressChanged?.Invoke(this, new TransferProgressEventArgs(message, percentage));
    }
}

/// <summary>
/// Event args for progress updates
/// </summary>
public class TransferProgressEventArgs : EventArgs
{
    public string Message { get; }
    public int Percentage { get; }

    public TransferProgressEventArgs(string message, int percentage)
    {
        Message = message;
        Percentage = Math.Clamp(percentage, 0, 100);
    }
}

/// <summary>
/// Log level enumeration
/// </summary>
internal enum LogLevel
{
    Debug,
    Information,
    Success,
    Warning,
    Error
}
