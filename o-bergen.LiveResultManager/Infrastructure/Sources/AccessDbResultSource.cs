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
                metadata.RaceNumber = day; // SUB field = race number in season series
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

        // Get metadata to access course definitions
        var metadata = await FetchMetadataAsync(cancellationToken);

        // Read basic results
        results = await ReadBasicResultsAsync(connection, cancellationToken);

        // Add split times to results
        await AddSplitTimesToResultsAsync(connection, results, metadata.Courses, cancellationToken);

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
    /// Reads all participants from Name table regardless of status, for duplicate detection
    /// </summary>
    public async Task<List<Core.Models.ParticipantEntry>> FetchAllParticipantsAsync(CancellationToken cancellationToken = default)
    {
        var participants = new List<Core.Models.ParticipantEntry>();

        using var connection = new OdbcConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new OdbcCommand("SELECT id, name, ename, ecard, class FROM Name", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            participants.Add(new Core.Models.ParticipantEntry(
                Id: GetString(reader, "id"),
                FirstName: GetString(reader, "name"),
                LastName: GetString(reader, "ename"),
                ECard: GetString(reader, "ecard"),
                Class: GetString(reader, "class")));
        }

        return participants;
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

    private async Task<Dictionary<string, string>> ReadJokerMappingsAsync(
        OdbcConnection connection,
        CancellationToken cancellationToken)
    {
        var jokerMap = new Dictionary<string, string>();

        try
        {
            var query = "SELECT joker, replace FROM joker";
            using var command = new OdbcCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var jokerCode = GetString(reader, "joker");
                var replaceCode = GetString(reader, "replace");

                if (!string.IsNullOrEmpty(jokerCode) && !string.IsNullOrEmpty(replaceCode))
                {
                    // Map physical code (replace) to course code (joker)
                    // Example: 69 -> 53 means when we see 69 in ecard, treat it as 53
                    jokerMap[replaceCode] = jokerCode;
                }
            }
        }
        catch
        {
            // If joker table doesn't exist or query fails, return empty map
        }

        return jokerMap;
    }

    private async Task AddSplitTimesToResultsAsync(
        OdbcConnection connection,
        List<RaceResult> results,
        List<Course> courses,
        CancellationToken cancellationToken)
    {
        // Read joker mappings first (maps physical code -> course code)
        var jokerMap = await ReadJokerMappingsAsync(connection, cancellationToken);

        // Build a dictionary of course code -> set of control codes for quick lookup
        var courseControlCodes = courses.ToDictionary(
            c => c.Level,
            c => new HashSet<string>(c.Controls.Select(ctrl => ctrl.Code)));

        // Read all split times into memory (more efficient than per-result queries)
        var splitsByECard = new Dictionary<int, List<SplitTime>>();

        var query = "SELECT ecardno, nr, control, times FROM ecard";
        using var command = new OdbcCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var ecardNo = GetInt(reader, "ecardno");
            var number = GetInt(reader, "nr");
            var controlCode = GetString(reader, "control");

            var split = new SplitTime
            {
                Number = number,
                Code = controlCode,
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
                // Get the control codes for this result's course
                var courseCode = result.Course;
                HashSet<string>? controlCodesForCourse = null;
                if (!string.IsNullOrEmpty(courseCode))
                {
                    courseControlCodes.TryGetValue(courseCode, out controlCodesForCourse);
                }

                // Apply course-aware joker mapping
                var mappedSplits = splits.Select(s =>
                {
                    var code = s.Code;
                    // Only apply joker mapping if:
                    // 1. The physical code (s.Code) has a joker mapping
                    // 2. The course controls exist and contain the joker code
                    // 3. The course controls do NOT contain the physical code
                    if (jokerMap.TryGetValue(code, out var jokerCode) && 
                        controlCodesForCourse != null &&
                        controlCodesForCourse.Contains(jokerCode) &&
                        !controlCodesForCourse.Contains(code))
                    {
                        code = jokerCode;
                    }

                    return new SplitTime
                    {
                        Number = s.Number,
                        Code = code,
                        Totaltime = s.Totaltime,
                        Splittime = s.Splittime
                    };
                }).ToList();

                // Sort by number and remove control 250 (finish)
                var sortedSplits = mappedSplits
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

    /// <summary>
    /// Merges a duplicate participant into a target participant.
    /// Updates the multi table so the removed ID's day entries point to the kept ID,
    /// then deletes the removed participant from the Name table.
    /// Handles the case where both IDs have entries for the same day (composite key id+day).
    /// Uses DELETE+INSERT instead of UPDATE since multi.id is part of the composite PK
    /// and cannot be updated via ODBC.
    /// </summary>
    public async Task MergeDuplicateAsync(string keepId, string removeId, CancellationToken cancellationToken = default)
    {
        using var connection = new OdbcConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Step 1: Find which days the keepId already covers
        var keepDays = new HashSet<int>();
        using (var cmd = new OdbcCommand("SELECT day FROM multi WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(keepId));
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                keepDays.Add(GetInt(reader, "day"));
        }

        // Step 2: Find which days the removeId has
        var removeDays = new List<int>();
        using (var cmd = new OdbcCommand("SELECT day FROM multi WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                removeDays.Add(GetInt(reader, "day"));
        }

        // Step 3: For days that keepId doesn't have yet, copy the row to keepId then delete original.
        // Cannot use UPDATE since multi.id is part of the composite primary key (id, day).
        foreach (var day in removeDays.Where(d => !keepDays.Contains(d)))
            await CopyMultiRowAsync(connection, removeId, keepId, day, cancellationToken);

        // Step 4: Delete remaining removeId entries (days already covered by keepId, or already copied)
        using (var cmd = new OdbcCommand("DELETE FROM multi WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Step 5: Delete the removed participant from Name table
        using (var cmd = new OdbcCommand("DELETE FROM Name WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Finds a safe temporary ID that does not conflict with any existing ID in the Name table.
    /// Returns (max numeric ID + 1) as a string.
    /// </summary>
    public async Task<string> FindSafeTempIdAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new OdbcConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var maxId = 0;
        using var cmd = new OdbcCommand("SELECT id FROM Name", connection);
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var raw = GetString(reader, "id");
            if (int.TryParse(raw, out var n) && n > maxId)
                maxId = n;
        }

        return (maxId + 1).ToString();
    }

    /// <summary>
    /// "Overfør fra ny": beholder keepId sitt løpernummer men kopierer alle Name-felt
    /// (unntatt id) fra removeId til keepId via UPDATE.
    /// Note: Name.id er AutoNumber i Access og kan ikke oppdateres via ODBC.
    /// </summary>
    public async Task SwapMergeDuplicateAsync(string keepId, string removeId, CancellationToken cancellationToken = default)
    {
        using var connection = new OdbcConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Phase 1: Read all fields from removeId's Name row
        List<string> removeColumns;
        List<object?> removeValues;
        using (var cmd = new OdbcCommand("SELECT * FROM Name WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException($"Participant #{removeId} not found in Name table.");

            var count = reader.FieldCount;
            removeColumns = new List<string>(count);
            removeValues  = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                removeColumns.Add(reader.GetName(i));
                removeValues.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
            }
        }

        // Phase 2: Merge non-conflicting multi rows from removeId onto keepId
        var keepDays = new HashSet<int>();
        using (var cmd = new OdbcCommand("SELECT day FROM multi WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(keepId));
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                keepDays.Add(GetInt(reader, "day"));
        }

        var removeDays = new List<int>();
        using (var cmd = new OdbcCommand("SELECT day FROM multi WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                removeDays.Add(GetInt(reader, "day"));
        }

        foreach (var day in removeDays.Where(d => !keepDays.Contains(d)))
            await CopyMultiRowAsync(connection, removeId, keepId, day, cancellationToken);

        // Phase 3: Delete removeId entirely BEFORE updating keepId.
        // This avoids unique-index violations (e.g. on ecard, bib) that would occur
        // if both rows exist simultaneously with the same field values during the UPDATE.
        using (var cmd = new OdbcCommand("DELETE FROM multi WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        using (var cmd = new OdbcCommand("DELETE FROM Name WHERE id = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(removeId));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Phase 4: UPDATE keepId's Name row with all fields from removeId (except id).
        // removeId is now deleted, so no unique-index conflict can occur.
        var setClauses = new List<string>();
        var setValues  = new List<object?>();
        for (var i = 0; i < removeColumns.Count; i++)
        {
            if (removeColumns[i].Equals("id", StringComparison.OrdinalIgnoreCase))
                continue;
            setClauses.Add($"[{removeColumns[i]}] = ?");
            setValues.Add(removeValues[i]);
        }

        if (setClauses.Count > 0)
        {
            using var cmd = new OdbcCommand(
                $"UPDATE Name SET {string.Join(", ", setClauses)} WHERE id = ?", connection);
            foreach (var val in setValues)
                cmd.Parameters.AddWithValue("@p", val ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@keepId", IdParam(keepId));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Converts a string participant ID to the appropriate numeric type for ODBC parameters.
    /// Access DB stores participant IDs as Long Integer; passing a string causes implicit
    /// conversion that is driver-dependent. This ensures we pass the correct type.
    /// </summary>
    private static object IdParam(string id) =>
        int.TryParse(id, out var n) ? (object)n : id;

    /// <summary>
    /// Copies a single multi row
    /// Used instead of UPDATE since multi.id is part of the composite primary key (id, day)
    /// and cannot be updated via ODBC.
    /// All columns are read dynamically so no schema knowledge is required.
    /// </summary>
    private static async Task CopyMultiRowAsync(
        OdbcConnection connection, string fromId, string toId, int day, CancellationToken ct)
    {
        List<string> columns;
        List<object?> values;

        using (var cmd = new OdbcCommand("SELECT * FROM multi WHERE id = ? AND day = ?", connection))
        {
            cmd.Parameters.AddWithValue("@id", IdParam(fromId));
            cmd.Parameters.AddWithValue("@day", day);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return;

            var count = reader.FieldCount;
            columns = new List<string>(count);
            values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                columns.Add(reader.GetName(i));
                values.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
            }
        }

        // Override id column with toId, preserving the original numeric type if applicable
        var idIdx = columns.FindIndex(c => c.Equals("id", StringComparison.OrdinalIgnoreCase));
        if (idIdx >= 0)
        {
            var original = values[idIdx];
            values[idIdx] = original switch
            {
                int    => int.TryParse(toId, out var i32) ? (object)i32 : toId,
                long   => long.TryParse(toId, out var i64) ? (object)i64 : toId,
                short  => short.TryParse(toId, out var i16) ? (object)i16 : toId,
                _      => IdParam(toId)   // fall back to IdParam for unknown numeric types
            };
        }

        var colList = string.Join(", ", columns.Select(c => $"[{c}]"));
        var paramList = string.Join(", ", columns.Select(_ => "?"));
        using (var insertCmd = new OdbcCommand($"INSERT INTO multi ({colList}) VALUES ({paramList})", connection))
        {
            foreach (var val in values)
                insertCmd.Parameters.AddWithValue("@p", val ?? DBNull.Value);
            await insertCmd.ExecuteNonQueryAsync(ct);
        }

        using var deleteCmd = new OdbcCommand("DELETE FROM multi WHERE id = ? AND day = ?", connection);
        deleteCmd.Parameters.AddWithValue("@id", IdParam(fromId));
        deleteCmd.Parameters.AddWithValue("@day", day);
        await deleteCmd.ExecuteNonQueryAsync(ct);
    }
}
