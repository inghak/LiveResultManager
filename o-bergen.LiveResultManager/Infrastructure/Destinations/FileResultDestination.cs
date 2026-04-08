using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Infrastructure.Destinations;

/// <summary>
/// File-based result destination for testing without Supabase
/// Writes results to a simple text file
/// </summary>
public class FileResultDestination : IResultDestination
{
    private readonly string _outputPath;

    public string DestinationName => "File Output (Testing)";

    public FileResultDestination(string outputPath)
    {
        _outputPath = outputPath ?? "test-output.txt";
    }

    public async Task<int> WriteResultsAsync(IReadOnlyList<RaceResult> results, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>
        {
            $"=== Results Export at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===",
            $"Total Results: {results.Count}",
            ""
        };

        foreach (var result in results)
        {
            var fullName = $"{result.FirstName} {result.LastName}".Trim();
            var timeOrStatus = !string.IsNullOrEmpty(result.Time) ? result.Time : result.Status;

            lines.Add($"{fullName,-30} {result.Class,-10} {result.Course,-15} " +
                     $"{timeOrStatus,-10} {result.Points,3} pts");
        }

        await File.WriteAllLinesAsync(_outputPath, lines, cancellationToken);
        return results.Count;
    }

    public Task<int> GetRecordCountAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_outputPath))
            return Task.FromResult(0);

        var lines = File.ReadAllLines(_outputPath);
        var countLine = lines.FirstOrDefault(l => l.StartsWith("Total Results:"));
        if (countLine != null && int.TryParse(countLine.Replace("Total Results:", "").Trim(), out var count))
            return Task.FromResult(count);

        return Task.FromResult(0);
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<int> DeleteResultsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    private static string FormatTime(int seconds)
    {
        var minutes = seconds / 60;
        var secs = seconds % 60;
        return $"{minutes}:{secs:D2}";
    }
}
