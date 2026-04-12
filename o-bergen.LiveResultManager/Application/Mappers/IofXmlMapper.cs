using System.Xml.Linq;
using o_bergen.LiveResultManager.Core.Models;
using LiveResultManager.Core.Enums;

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

            // Start and End times - use full datetime if available from database
            if (eventMetadata.StartTime.HasValue)
            {
                // Create StartTime with both Date and Time elements
                var startTimeElement = new XElement(_ns + "StartTime",
                    new XElement(_ns + "Date", eventMetadata.StartTime.Value.ToString("yyyy-MM-dd")),
                    new XElement(_ns + "Time", FormatTime(eventMetadata.StartTime.Value))
                );
                eventElement.Add(startTimeElement);

                // Create EndTime with both Date and Time elements
                var endTime = eventMetadata.EndTime ?? eventMetadata.StartTime.Value;
                var endTimeElement = new XElement(_ns + "EndTime",
                    new XElement(_ns + "Date", endTime.ToString("yyyy-MM-dd")),
                    new XElement(_ns + "Time", FormatTime(endTime))
                );
                eventElement.Add(endTimeElement);
            }
            else if (!string.IsNullOrEmpty(eventMetadata.Date))
            {
                // Fallback: date-only if StartTime not available
                if (DateTime.TryParse(eventMetadata.Date, out var eventDate))
                {
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

        // Course element (lookup from metadata by course code/level)
        var firstResult = results.FirstOrDefault();
        if (firstResult != null && !string.IsNullOrEmpty(firstResult.Course))
        {
            // Try to find course in metadata by matching Level (course code)
            var course = metadata.EventMetadata?.Courses
                .FirstOrDefault(c => c.Level == firstResult.Course);

            if (course != null && course.Length > 0)
            {
                // Use course length from metadata
                classResult.Add(new XElement(_ns + "Course",
                    new XElement(_ns + "Length", (int)course.Length)
                ));
            }
            else if (int.TryParse(firstResult.Course, out var courseLength) && courseLength > 10)
            {
                // Fallback: if Course field is actually a length (> 10 to avoid course codes)
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

        // Start and Finish times (use event start time or individual start time)
        DateTime? actualStartTime = null;

        // First, try to use the event's actual StartTime from metadata
        if (metadata.EventMetadata?.StartTime.HasValue == true)
        {
            actualStartTime = metadata.EventMetadata.StartTime.Value;
        }
        // Fallback: parse from date string with default time
        else if (metadata.EventMetadata != null && !string.IsNullOrEmpty(metadata.EventMetadata.Date))
        {
            if (DateTime.TryParse(metadata.EventMetadata.Date, out var eventDate))
            {
                actualStartTime = eventDate.Date.AddHours(17); // Default 17:00
            }
        }

        if (actualStartTime.HasValue)
        {
            resultElement.Add(new XElement(_ns + "StartTime", FormatDateTime(actualStartTime.Value)));

            // Calculate finish time from result time
            var timeSeconds = ParseTimeToSeconds(result.Time);
            if (timeSeconds > 0)
            {
                var finishTime = actualStartTime.Value.AddSeconds(timeSeconds);
                resultElement.Add(new XElement(_ns + "FinishTime", FormatDateTime(finishTime)));
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

        // Course (lookup from metadata by course code/level)
        if (!string.IsNullOrEmpty(result.Course))
        {
            // Try to find course in metadata by matching Level (course code)
            var course = metadata.EventMetadata?.Courses
                .FirstOrDefault(c => c.Level == result.Course);

            if (course != null && course.Length > 0)
            {
                // Use course length from metadata
                resultElement.Add(new XElement(_ns + "Course",
                    new XElement(_ns + "Length", (int)course.Length)
                ));
            }
            else if (int.TryParse(result.Course, out var courseLength) && courseLength > 10)
            {
                // Fallback: if Course field is actually a length (> 10 to avoid course codes)
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
        return StatusMapper.IsOkStatus(status);
    }

    private string MapStatus(string? status)
    {
        var competitorStatus = StatusMapper.ParseStatus(status);
        return StatusMapper.ToIofXmlString(competitorStatus);
    }

    private string FormatDateTime(DateTime dateTime)
    {
        // ISO 8601 format with timezone
        return dateTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    private string FormatTime(DateTime dateTime)
    {
        // IOF XML Time format: HH:mm:ss+TZ (e.g., 17:00:00+02:00)
        return dateTime.ToString("HH:mm:sszzz");
    }
}
