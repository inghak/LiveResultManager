namespace LiveResultManager.Core.Enums;

/// &lt;summary&gt;
/// Maps between Norwegian eTiming database status codes and IOF XML standard status values.
/// &lt;/summary&gt;
public static class StatusMapper
{
    /// &lt;summary&gt;
    /// Maps a Norwegian eTiming status code to the corresponding IOF CompetitorStatus.
    /// &lt;/summary&gt;
    /// &lt;param name="eTimingStatus"&gt;The eTiming status code.&lt;/param&gt;
    /// &lt;returns&gt;The corresponding IOF CompetitorStatus.&lt;/returns&gt;
    public static CompetitorStatus ToIofStatus(ETimingStatus eTimingStatus)
    {
        return eTimingStatus switch
        {
            ETimingStatus.A => CompetitorStatus.OK,
            ETimingStatus.B => CompetitorStatus.DidNotFinish,  // Brutt (broken/abandoned)
            ETimingStatus.C => CompetitorStatus.DidNotStart,
            ETimingStatus.D => CompetitorStatus.Disqualified,
            ETimingStatus.S => CompetitorStatus.Unknown,  // Still in the woods ("i skogen")
            ETimingStatus.Unknown => CompetitorStatus.Unknown,
            _ => CompetitorStatus.DidNotStart
        };
    }
    
    /// &lt;summary&gt;
    /// Maps a string status code to IOF CompetitorStatus.
    /// Handles both eTiming codes and common status strings.
    /// &lt;/summary&gt;
    /// &lt;param name="statusString"&gt;The status string from the database or other source.&lt;/param&gt;
    /// &lt;returns&gt;The corresponding IOF CompetitorStatus.&lt;/returns&gt;
    public static CompetitorStatus ParseStatus(string? statusString)
    {
        if (string.IsNullOrWhiteSpace(statusString))
            return CompetitorStatus.DidNotStart;

        var normalized = statusString.Trim().ToUpperInvariant();

        // Try to parse as eTiming code first
        if (normalized.Length == 1 && Enum.TryParse<ETimingStatus>(normalized, out var eTimingStatus))
        {
            return ToIofStatus(eTimingStatus);
        }

        // Handle common status strings
        return normalized switch
        {
            "OK" => CompetitorStatus.OK,
            "FINISHED" => CompetitorStatus.OK,
            "DISQUALIFIED" => CompetitorStatus.Disqualified,
            "MISPUNCH" => CompetitorStatus.MissingPunch,
            "DNF" => CompetitorStatus.DidNotFinish,
            "DNS" => CompetitorStatus.DidNotStart,
            "OVERTIME" => CompetitorStatus.OverTime,
            "NC" => CompetitorStatus.NotCompeting,
            "CANCELLED" => CompetitorStatus.Cancelled,
            "INACTIVE" => CompetitorStatus.Inactive,
            "MOVED" => CompetitorStatus.Moved,
            "MOVEDUP" => CompetitorStatus.MovedUp,
            _ => CompetitorStatus.Unknown
        };
    }
    
    /// &lt;summary&gt;
    /// Converts a CompetitorStatus to its IOF XML string representation.
    /// &lt;/summary&gt;
    /// &lt;param name="status"&gt;The CompetitorStatus enum value.&lt;/param&gt;
    /// &lt;returns&gt;The IOF XML status string.&lt;/returns&gt;
    public static string ToIofXmlString(CompetitorStatus status)
    {
        return status.ToString();
    }
    
    /// &lt;summary&gt;
    /// Checks if a status represents a successful finish (OK).
    /// &lt;/summary&gt;
    /// &lt;param name="status"&gt;The CompetitorStatus to check.&lt;/param&gt;
    /// &lt;returns&gt;True if the status is OK, false otherwise.&lt;/returns&gt;
    public static bool IsOkStatus(CompetitorStatus status)
    {
        return status == CompetitorStatus.OK;
    }
    
    /// &lt;summary&gt;
    /// Checks if a status string represents a successful finish (OK).
    /// &lt;/summary&gt;
    /// &lt;param name="statusString"&gt;The status string to check.&lt;/param&gt;
    /// &lt;returns&gt;True if the status represents OK, false otherwise.&lt;/returns&gt;
    public static bool IsOkStatus(string? statusString)
    {
        if (string.IsNullOrWhiteSpace(statusString))
            return false;

        var status = ParseStatus(statusString);
        return IsOkStatus(status);
    }
}
