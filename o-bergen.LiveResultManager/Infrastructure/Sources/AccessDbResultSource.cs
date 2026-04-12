using System.Data.Common;
using System.Data.Odbc;
using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;
using CoreControl = o_bergen.LiveResultManager.Core.Models.Control;

namespace o_bergen.LiveResultManager.Infrastructure.Sources;

/// <summary>
/// Reads race results from Microsoft Access Database using ODBC
/// Implements IResultSource for the Repository pattern
/// </summary>
public class AccessDbResultSource : IResultSource
{
    private readonly string _connectionString;
    private readonly string _dbPath;
    private EventMetadata? _cachedMetadata;

    public string SourceName => "Access Database";

    /// <summary>
    /// Creates a new AccessDbResultSource
    /// </summary>
    /// <param name="dbPath">Path to the Access database file (.mdb or .accdb)</param>
    public AccessDbResultSource(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        _connectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={dbPath};";
    }

    /// <summary>
    /// Reads event metadata from Access database
    /// </summary>
    public async Task<EventMetadata> FetchMetadataAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedMetadata != null)
            return _cachedMetadata;

        var metadata = new EventMetadata();

        using var connection = new OdbcConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Read from arr table
        using (var command = new OdbcCommand("SELECT SUB, firststart, Organizator, eventplace FROM arr", connection))
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var day = GetInt(reader, "SUB");
                var arrDate = GetString(reader, "firststart");
                var arrPlace = GetString(reader, "eventplace");

                metadata.Day = day;
                metadata.Organizer = GetString(reader, "Organizator");
                metadata.Location = arrPlace;

                // Parse date and time from firststart
                if (DateTime.TryParseExact(arrDate, "dd.MM.yyyy HH:mm:ss", null, 
                    System.Globalization.DateTimeStyles.None, out var datetime))
                {
                    metadata.Year = datetime.Year.ToString();
                    metadata.Date = datetime.ToString("yyyy-MM-dd");
                    metadata.StartTime = datetime;
                    metadata.EndTime = datetime; // Same as start for now; could be calculated if needed
                }
                else
                {
                    metadata.Year = DateTime.Now.Year.ToString();
                    metadata.Date = DateTime.Now.ToString("yyyy-MM-dd");
                }
            }
        }

        // Read courses
        var courses = new List<Course>();
        using (var command = new OdbcCommand("SELECT code, name, length FROM cource", connection))
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var course = new Course
                {
                    Level = GetString(reader, "code"),
                    Name = GetString(reader, "name"),
                    Length = GetDouble(reader, "length")
                };
                courses.Add(course);
            }
        }

        // Read controls and assign to courses
        using (var command = new OdbcCommand("SELECT courceno, controlno, code FROM controls", connection))
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var courseNo = GetString(reader, "courceno");
                var controlNo = GetInt(reader, "controlno");
                var code = GetString(reader, "code");

                var control = new CoreControl
                {
                    No = controlNo,
                    Code = code
                };

                var course = courses.FirstOrDefault(c => c.Level == courseNo);
                if (course != null && !course.Controls.Any(c => c.Code == control.Code))
                {
                    course.Controls.Add(control);
                }
            }
        }

        metadata.Courses = courses;
        _cachedMetadata = metadata;

        return metadata;
    }

    /// <summary>
    /// Reads all race results from the Access database
    /// </summary>
    public async Task<IReadOnlyList<RaceResult>> ReadResultsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<RaceResult>();

        using var connection = new OdbcConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Read basic results
        results = await ReadBasicResultsAsync(connection, cancellationToken);

        // Add split times to results
        await AddSplitTimesToResultsAsync(connection, results, cancellationToken);

        return results;
    }

    /// <summary>
    /// Gets the last modified timestamp of the database file
    /// </summary>
    public Task<DateTime?> GetLastModifiedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_dbPath))
                return Task.FromResult<DateTime?>(null);

            var lastModified = File.GetLastWriteTime(_dbPath);
            return Task.FromResult<DateTime?>(lastModified);
        }
        catch
        {
            return Task.FromResult<DateTime?>(null);
        }
    }

    /// <summary>
    /// Tests if the database is accessible
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_dbPath))
                return false;

            using var connection = new OdbcConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<RaceResult>> ReadBasicResultsAsync(
        OdbcConnection connection, 
        CancellationToken cancellationToken)
    {
        var results = new List<RaceResult>();

        // Query matches the BackgroundSource SQL
        // Filter by eTiming status codes: A=OK, B=DidNotFinish(brutt), D=Disqualified, S=Unknown(i skogen)
        // Note: C=DidNotStart is excluded as those competitors never started
        var query = @"
            SELECT 
                n.id, 
                n.ecard, 
                n.ecard2, 
                n.ename, 
                n.name, 
                n.starttime, 
                n.times, 
                n.status, 
                n.statusmsg, 
                n.class, 
                n.cource, 
                n.points, 
                n.team, 
                t.name as teamname 
            FROM Name n 
            LEFT JOIN Team t ON n.team = t.code
            WHERE n.status IN ('A','D','B','S')";

        using var command = new OdbcCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var result = new RaceResult
            {
                Id = GetString(reader, "id"),
                ECard = GetString(reader, "ecard"),
                ECard2 = GetStringOrNull(reader, "ecard2"),
                FirstName = GetString(reader, "name"),      // name = given name (fornavn)
                LastName = GetString(reader, "ename"),      // ename = family name (etternavn)
                StartTime = ConvertStartTimeToHHMM(reader, "starttime"),
                Time = GetString(reader, "times"),
                Status = GetString(reader, "status"),
                StatusMessage = GetStringOrNull(reader, "statusmsg"),
                Class = GetString(reader, "class"),
                Course = GetString(reader, "cource"),
                Points = GetString(reader, "points"),
                TeamId = GetStringOrNull(reader, "team"),
                TeamName = GetStringOrNull(reader, "teamname")
            };

            results.Add(result);
        }

        return results;
    }

    private async Task AddSplitTimesToResultsAsync(
        OdbcConnection connection,
        List<RaceResult> results,
        CancellationToken cancellationToken)
    {
        // Read all split times into memory (more efficient than per-result queries)
        var splitsByECard = new Dictionary<int, List<SplitTime>>();

        var query = "SELECT ecardno, nr, control, times FROM ecard";
        using var command = new OdbcCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var ecardNo = GetInt(reader, "ecardno");
            var number = GetInt(reader, "nr");
            var split = new SplitTime
            {
                Number = number,
                Code = GetString(reader, "control"),
                Totaltime = GetInt(reader, "times"),
                Splittime = -1 // Will be calculated later
            };

            if (!splitsByECard.ContainsKey(ecardNo))
                splitsByECard[ecardNo] = new List<SplitTime>();

            splitsByECard[ecardNo].Add(split);
        }

        // Assign splits to results and calculate split times
        foreach (var result in results)
        {
            // Prefer ECard2 (rental) if set, otherwise use ECard
            var ecardNo = int.TryParse(result.ECard2, out var e2) && e2 > 0 
                ? e2 
                : int.TryParse(result.ECard, out var e1) ? e1 : -1;

            if (ecardNo > 0 && splitsByECard.TryGetValue(ecardNo, out var splits))
            {
                // Sort by number and remove control 250 (finish)
                var sortedSplits = splits
                    .Where(s => s.Code != "250")
                    .OrderBy(s => s.Number)
                    .ToList();

                // Calculate split times (time from previous control)
                if (sortedSplits.Count > 0)
                {
                    sortedSplits[0].Splittime = sortedSplits[0].Totaltime;

                    for (int i = 1; i < sortedSplits.Count; i++)
                    {
                        sortedSplits[i].Splittime = 
                            sortedSplits[i].Totaltime - sortedSplits[i - 1].Totaltime;
                    }
                }

                result.SplitTimes = sortedSplits;
            }
        }
    }

    private static string GetString(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return string.Empty;

        // Use GetValue and ToString to handle any type from Access DB
        var value = reader.GetValue(ordinal);
        return value?.ToString()?.Trim() ?? string.Empty;
    }

    private static string? GetStringOrNull(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        // Use GetValue and ToString to handle any type from Access DB
        var value = reader.GetValue(ordinal);
        var stringValue = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(stringValue) ? null : stringValue;
    }

    private static int GetInt(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return 0;

        var value = reader.GetValue(ordinal);
        return value switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            byte b => b,
            decimal d => (int)d,
            double dbl => (int)dbl,
            float f => (int)f,
            string str when int.TryParse(str, out var result) => result,
            _ => 0
        };
    }

    private static double GetDouble(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return 0;

        var value = reader.GetValue(ordinal);
        return value switch
        {
            double d => d,
            float f => f,
            decimal dec => (double)dec,
            int i => i,
            long l => l,
            short s => s,
            byte b => b,
            string str when double.TryParse(str, out var result) => result,
            _ => 0
        };
    }

    private static string? ConvertStartTimeToHHMM(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetValue(ordinal);

        // Handle fraction of a day (Access Database datetime format)
        double fractionOfDay = value switch
        {
            double d => d,
            float f => f,
            decimal dec => (double)dec,
            string str when double.TryParse(str, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var result) => result,
            _ => -1
        };

        if (fractionOfDay >= 0 && fractionOfDay < 1)
        {
            // Convert fraction of day to hours and minutes
            var totalMinutes = (int)(fractionOfDay * 24 * 60);
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            return $"{hours:D2}:{minutes:D2}";
        }

        return null;
    }
}
