using System.Xml.Linq;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Application.Mappers;

/// <summary>
/// Maps domain models to IOF XML 3.0 format
/// International Orienteering Federation Data Standard
/// </summary>
public class IofXmlMapper
{
    private const string IofNamespace = "http://www.orienteering.org/datastandard/3.0";
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    private readonly XNamespace _ns;
    private readonly XNamespace _xsi;

    public IofXmlMapper()
    {
        _ns = IofNamespace;
        _xsi = XsiNamespace;
    }

    /// <summary>
    /// Creates a complete IOF XML 3.0 ResultList document
    /// </summary>
    public XDocument CreateResultList(
        IReadOnlyList<RaceResult> results,
        ResultMetadata metadata)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            CreateResultListElement(results, metadata)
        );

        return doc;
    }

    private XElement CreateResultListElement(
        IReadOnlyList<RaceResult> results,
        ResultMetadata metadata)
    {
        var resultList = new XElement(_ns + "ResultList",
            new XAttribute("iofVersion", "3.0"),
            new XAttribute("createTime", FormatDateTime(metadata.TransferDate)),
            new XAttribute("creator", "LiveResultManager"),
            new XAttribute("status", "Complete"),
            new XAttribute(XNamespace.Xmlns + "xsi", XsiNamespace)
        );

        // Add Event element
        resultList.Add(CreateEventElement(metadata.EventMetadata));

        // Group results by class and add ClassResult elements
        var resultsByClass = results
            .GroupBy(r => r.Class?.Trim() ?? "Unknown")
            .OrderBy(g => g.Key);

        foreach (var classGroup in resultsByClass)
        {
            resultList.Add(CreateClassResultElement(classGroup.Key, classGroup.ToList(), metadata));
        }

        return resultList;
    }

    private XElement CreateEventElement(EventMetadata? eventMetadata)
    {
        var eventElement = new XElement(_ns + "Event");

        if (eventMetadata != null)
        {
            // Id (optional, can be empty - not in our metadata)
            eventElement.Add(new XElement(_ns + "Id", string.Empty));

            // Name
            eventElement.Add(new XElement(_ns + "Name", eventMetadata.Name ?? "Bedriftsløp"));

            // Start and End times - estimate from date only since we don't have times
            if (!string.IsNullOrEmpty(eventMetadata.Date))
            {
                if (DateTime.TryParse(eventMetadata.Date, out var eventDate))
                {
                    // Create basic date-only elements (no time since we don't have it in metadata)
                    eventElement.Add(new XElement(_ns + "StartTime",
                        new XElement(_ns + "Date", eventDate.ToString("yyyy-MM-dd"))
                    ));
                    eventElement.Add(new XElement(_ns + "EndTime",
                        new XElement(_ns + "Date", eventDate.ToString("yyyy-MM-dd"))
                    ));
                }
            }
        }
        else
        {
            // Fallback if no metadata
            eventElement.Add(new XElement(_ns + "Id", string.Empty));
            eventElement.Add(new XElement(_ns + "Name", "Bedriftsløp"));
        }

        return eventElement;
    }

    private XElement CreateClassResultElement(
        string className,
        List<RaceResult> results,
        ResultMetadata metadata)
    {
        var classResult = new XElement(_ns + "ClassResult");

        // Class element
        classResult.Add(new XElement(_ns + "Class",
            new XElement(_ns + "Name", className)
        ));

        // Course element (take from first result if available)
        var firstResult = results.FirstOrDefault();
        if (firstResult != null && !string.IsNullOrEmpty(firstResult.Course))
        {
            if (int.TryParse(firstResult.Course, out var courseLength))
            {
                classResult.Add(new XElement(_ns + "Course",
                    new XElement(_ns + "Length", courseLength)
                ));
            }
        }

        // Calculate positions and times behind for OK results
        var okResults = results
            .Where(r => IsOkStatus(r.Status))
            .OrderBy(r => ParseTimeToSeconds(r.Time))
            .ToList();

        var winnerTime = okResults.FirstOrDefault() != null
            ? ParseTimeToSeconds(okResults.First().Time)
            : 0;

        // Assign positions
        var resultPositions = new Dictionary<RaceResult, (int position, int timeBehind)>();

        for (int i = 0; i < okResults.Count; i++)
        {
            var result = okResults[i];
            var timeSeconds = ParseTimeToSeconds(result.Time);
            var timeBehind = timeSeconds - winnerTime;
            resultPositions[result] = (i + 1, timeBehind);
        }

        // Add PersonResult elements - keep original order or sort by position
        var sortedResults = results.OrderBy(result =>
        {
            if (resultPositions.TryGetValue(result, out var pos))
                return pos.position;
            return int.MaxValue; // Non-OK results at the end
        });

        foreach (var result in sortedResults)
        {
            var hasPosition = resultPositions.TryGetValue(result, out var posInfo);
            classResult.Add(CreatePersonResultElement(result, hasPosition ? posInfo : (0, 0), metadata));
        }

        return classResult;
    }

    private XElement CreatePersonResultElement(
        RaceResult result,
        (int position, int timeBehind) positionInfo,
        ResultMetadata metadata)
    {
        var personResult = new XElement(_ns + "PersonResult");

        // Person element
        var (firstName, lastName) = SplitName(result.FirstName, result.LastName);
        personResult.Add(new XElement(_ns + "Person",
            string.IsNullOrWhiteSpace(result.Id)
                ? null
                : new XElement(_ns + "Id", result.Id.Trim()),
            new XElement(_ns + "Name",
                new XElement(_ns + "Family", lastName),
                new XElement(_ns + "Given", firstName)
            )
        ));

        // Organisation element
        if (!string.IsNullOrWhiteSpace(result.TeamName))
        {
            personResult.Add(new XElement(_ns + "Organisation",
                new XElement(_ns + "Name", result.TeamName.Trim()),
                new XElement(_ns + "Country",
                    new XAttribute("code", "NOR"),
                    "Norway"
                )
            ));
        }

        // Result element
        personResult.Add(CreateResultElement(result, positionInfo, metadata));

        return personResult;
    }

    private XElement CreateResultElement(
        RaceResult result,
        (int position, int timeBehind) positionInfo,
        ResultMetadata metadata)
    {
        var resultElement = new XElement(_ns + "Result");

        // BibNumber (use ECard if available)
        if (!string.IsNullOrWhiteSpace(result.ECard))
        {
            resultElement.Add(new XElement(_ns + "BibNumber", result.ECard));
        }
        else
        {
            resultElement.Add(new XElement(_ns + "BibNumber", "0"));
        }

        // Start and Finish times (estimate from event metadata if available)
        // Note: We don't have actual start times per person, so we estimate
        if (metadata.EventMetadata != null && !string.IsNullOrEmpty(metadata.EventMetadata.Date))
        {
            if (DateTime.TryParse(metadata.EventMetadata.Date, out var eventDate))
            {
                // Use a default start time (e.g., 17:00) since we don't have it in metadata
                var startTime = eventDate.Date.AddHours(17);

                resultElement.Add(new XElement(_ns + "StartTime", FormatDateTime(startTime)));

                // Calculate finish time
                var timeSeconds = ParseTimeToSeconds(result.Time);
                if (timeSeconds > 0)
                {
                    var finishTime = startTime.AddSeconds(timeSeconds);
                    resultElement.Add(new XElement(_ns + "FinishTime", FormatDateTime(finishTime)));
                }
            }
        }

        // Time in seconds
        var totalSeconds = ParseTimeToSeconds(result.Time);
        if (totalSeconds > 0)
        {
            resultElement.Add(new XElement(_ns + "Time", totalSeconds));
        }

        // TimeBehind
        if (positionInfo.position > 0)
        {
            resultElement.Add(new XElement(_ns + "TimeBehind", positionInfo.timeBehind));
        }

        // Position
        if (positionInfo.position > 0)
        {
            resultElement.Add(new XElement(_ns + "Position", positionInfo.position));
        }

        // Status
        resultElement.Add(new XElement(_ns + "Status", MapStatus(result.Status)));

        // Course (if specific to this result)
        if (!string.IsNullOrEmpty(result.Course))
        {
            if (int.TryParse(result.Course, out var courseLength))
            {
                resultElement.Add(new XElement(_ns + "Course",
                    new XElement(_ns + "Length", courseLength)
                ));
            }
        }

        // SplitTimes
        foreach (var split in result.SplitTimes.OrderBy(s => s.Number))
        {
            var splitElement = new XElement(_ns + "SplitTime",
                new XElement(_ns + "ControlCode", split.Code)
            );

            // Time is the total time to this control in seconds
            if (split.Totaltime > 0)
            {
                splitElement.Add(new XElement(_ns + "Time", split.Totaltime));
            }

            resultElement.Add(splitElement);
        }

        return resultElement;
    }

    private (string firstName, string lastName) SplitName(string firstName, string lastName)
    {
        // Handle cases where name might be in one field
        if (string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(firstName))
        {
            var parts = firstName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return (string.Join(" ", parts.Take(parts.Length - 1)), parts.Last());
            }
            return (firstName.Trim(), "Unknown");
        }

        return (
            string.IsNullOrWhiteSpace(firstName) ? "Unknown" : firstName.Trim(),
            string.IsNullOrWhiteSpace(lastName) ? "Unknown" : lastName.Trim()
        );
    }

    private int ParseTimeToSeconds(string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            return 0;

        // Try parse as integer (seconds)
        if (int.TryParse(timeString, out var seconds))
            return seconds;

        // Try parse as time format (mm:ss or hh:mm:ss)
        if (TimeSpan.TryParse(timeString, out var timeSpan))
            return (int)timeSpan.TotalSeconds;

        return 0;
    }

    private bool IsOkStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        var normalized = status.Trim().ToUpperInvariant();
        return normalized == "OK" || normalized == "FINISHED";
    }

    private string MapStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "DidNotStart";

        var normalized = status.Trim().ToUpperInvariant();

        return normalized switch
        {
            "OK" => "OK",
            "FINISHED" => "OK",
            "DISQUALIFIED" => "Disqualified",
            "MISPUNCH" => "MissingPunch",
            "DNF" => "DidNotFinish",
            "DNS" => "DidNotStart",
            "OVERTIME" => "OverTime",
            "NC" => "NotCompeting",
            _ => "DidNotStart"
        };
    }

    private string FormatDateTime(DateTime dateTime)
    {
        // ISO 8601 format with timezone
        return dateTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }
}
