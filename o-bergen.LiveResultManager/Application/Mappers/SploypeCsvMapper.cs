using System.Text;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Application.Mappers;

/// <summary>
/// Maps RaceResult domain models to World of O WinSplits Sploype.csv format
/// Format: Course header (CourseNumber,ControlCount) followed by runner rows
/// Runner format: Lastname,Firstname,Club(37 chars),StartTime(HH:MM),Split1(MM:SS),Split2(MM:SS),...
/// Note: Start time uses placeholder value as individual start times are not available in source data
/// Supports both Windows-1252 (legacy) and UTF-8 with BOM (modern) encoding
/// </summary>
public class SploypeCsvMapper
{
    private const int ClubFieldWidth = 37;

    /// <summary>
    /// Creates Sploype CSV content with optional UTF-8 BOM
    /// </summary>
    /// <param name="results">Race results to export</param>
    /// <param name="metadata">Event metadata</param>
    /// <param name="useUtf8Bom">If true, adds UTF-8 BOM at start. If false, no BOM (for Windows-1252)</param>
    /// <returns>CSV content as string</returns>
    public string CreateSploypeCsv(List<RaceResult> results, EventMetadata metadata, bool useUtf8Bom = false)
    {
        var sb = new StringBuilder();

        // Add UTF-8 BOM if requested (for modern systems)
        // Otherwise no BOM (for Windows-1252 / legacy WinSplits compatibility)
        if (useUtf8Bom)
        {
            sb.Append('\uFEFF');
        }

        // Group results by course and order by control count (descending)
        var groupedByCourse = results
            .GroupBy(r => r.Course)
            .Select(g => new
            {
                CourseName = g.Key,
                Results = g.ToList(),
                // SplitTimes includes start, but control count should exclude it
                ControlCount = g.Where(r => r.SplitTimes != null && r.SplitTimes.Any())
                               .Select(r => r.SplitTimes.Count - 1)  // -1 to exclude start
                               .FirstOrDefault()
            })
            .OrderByDescending(g => g.ControlCount)
            .ThenBy(g => g.CourseName)
            .ToList();

        int courseNumber = 1;
        foreach (var courseGroup in groupedByCourse)
        {
            var courseName = courseGroup.CourseName;
            var courseResults = courseGroup.Results.OrderBy(r => r.FirstName).ToList();
            var controlCount = courseGroup.ControlCount;

            if (courseResults.Count == 0)
                continue;

            // Write course header: CourseNumber,ControlCount
            // Note: ControlCount excludes start (which is included in SplitTimes data)
            sb.AppendLine($"{courseNumber},{controlCount}");

            // Write each runner in this course
            foreach (var result in courseResults)
            {
                WriteRunnerRow(sb, result);
            }

            courseNumber++;
        }

        return sb.ToString();
    }

    private void WriteRunnerRow(StringBuilder sb, RaceResult result)
    {
        // Format: Lastname,Firstname,Club(padded),StartTime(HH:MM),Split1(MM:SS),Split2(MM:SS),...

        var firstName = EscapeCsvField(result.FirstName);
        var lastName = EscapeCsvField(result.LastName);
        var club = PadClubName(result.TeamName ?? "");
        var startTime = result.StartTime ?? "17:00";

        // Output: Lastname,Firstname,Club,StartTime
        sb.Append($"{lastName},{firstName},{club},{startTime}");

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

    private string FormatSplitTime(int seconds)
    {
        // Convert seconds to MM:SS format
        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;
        return $"{minutes:D2}:{remainingSeconds:D2}";
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(',') || field.Contains('"'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private string PadClubName(string clubName)
    {
        if (string.IsNullOrEmpty(clubName))
            return new string(' ', ClubFieldWidth);

        if (clubName.Length >= ClubFieldWidth)
            return clubName.Substring(0, ClubFieldWidth);

        return clubName.PadRight(ClubFieldWidth);
    }

    private string ConvertStartTimeToHHMM(string? startTime)
    {
        if (string.IsNullOrEmpty(startTime))
            return "17:00";

        // Try to parse as fraction of a day (Access Database format)
        if (double.TryParse(startTime, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out var fractionOfDay))
        {
            // Convert fraction of day to hours and minutes
            var totalMinutes = (int)(fractionOfDay * 24 * 60);
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            return $"{hours:D2}:{minutes:D2}";
        }

        // If already in HH:MM format or other format, try to use it
        if (startTime.Contains(':'))
        {
            var parts = startTime.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                return $"{h:D2}:{m:D2}";
            }
        }

        // Default fallback
        return "17:00";
    }
}
