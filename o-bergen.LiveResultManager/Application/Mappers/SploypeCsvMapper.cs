using System.Text;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Application.Mappers;

/// <summary>
/// Maps RaceResult domain models to World of O WinSplits Sploype.csv format
/// Format: Class header (ClassNumber,ControlCount) followed by runner rows
/// Runner format: Firstname,Lastname,Club,StartTime(HH:MM),Split1(MM:SS),Split2(MM:SS),...
/// </summary>
public class SploypeCsvMapper
{
    public string CreateSploypeCsv(List<RaceResult> results, EventMetadata metadata)
    {
        var sb = new StringBuilder();
        
        // Group results by class
        var groupedByClass = results
            .GroupBy(r => r.Class)
            .OrderBy(g => GetClassNumber(g.Key))
            .ToList();

        int classNumber = 1;
        foreach (var classGroup in groupedByClass)
        {
            var className = classGroup.Key;
            var classResults = classGroup.OrderBy(r => r.FirstName).ToList();
            
            if (classResults.Count == 0)
                continue;

            // Get control count from first runner with splits
            var controlCount = classResults
                .Where(r => r.SplitTimes != null && r.SplitTimes.Any())
                .Select(r => r.SplitTimes.Count)
                .FirstOrDefault();

            // Write class header: ClassNumber,ControlCount
            sb.AppendLine($"{classNumber},{controlCount}");

            // Write each runner in this class
            foreach (var result in classResults)
            {
                WriteRunnerRow(sb, result);
            }

            classNumber++;
        }

        return sb.ToString();
    }

    private void WriteRunnerRow(StringBuilder sb, RaceResult result)
    {
        // Format: Firstname,Lastname,Club,StartTime(HH:MM),Split1(MM:SS),Split2(MM:SS),...
        
        var firstName = EscapeCsvField(result.FirstName);
        var lastName = EscapeCsvField(result.LastName);
        var club = EscapeCsvField(result.TeamName ?? "");
        
        // Start time - extract from first split if available
        var startTime = "00:00";
        if (result.SplitTimes != null && result.SplitTimes.Any())
        {
            // For World of O format, we typically use HH:MM format
            // This would need actual start time from event, defaulting to 00:00
            startTime = "00:00";
        }

        sb.Append($"{firstName},{lastName},{club},{startTime}");

        // Add split times in MM:SS format
        if (result.SplitTimes != null)
        {
            foreach (var split in result.SplitTimes.OrderBy(s => s.Number))
            {
                var splitTimeFormatted = FormatSplitTime(split.Splittime);
                sb.Append($",{splitTimeFormatted}");
            }
        }

        sb.AppendLine();
    }

    private string FormatSplitTime(int milliseconds)
    {
        // Convert milliseconds to MM:SS format
        var totalSeconds = milliseconds / 1000;
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Pad club name to match World of O format (appears to be 37 chars in example)
        // But simpler approach: just escape if contains comma or quotes
        if (field.Contains(',') || field.Contains('"'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private int GetClassNumber(string className)
    {
        // Simple ordering - could be enhanced with specific class ordering logic
        // For now, alphabetical ordering
        return className.GetHashCode();
    }
}
