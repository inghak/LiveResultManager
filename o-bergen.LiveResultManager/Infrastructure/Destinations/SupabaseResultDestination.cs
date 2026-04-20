using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;

namespace o_bergen.LiveResultManager.Infrastructure.Destinations;

/// <summary>
/// Writes race results to Supabase database
/// Matches existing LiveResultsService implementation
/// </summary>
public class SupabaseResultDestination : IResultDestination
{
    private readonly string _url;
    private readonly string _apiKey;
    private Client? _client;
    private string _competitionDate = string.Empty;

    public string DestinationName => "Supabase";

    public SupabaseResultDestination(string url, string apiKey)
    {
        _url = url ?? throw new ArgumentNullException(nameof(url));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    /// <summary>
    /// Upload competition metadata to live_competitions table
    /// Must be called before WriteResultsAsync to set competition date
    /// </summary>
    public async Task UploadMetadataAsync(EventMetadata metadata)
    {
        await EnsureInitializedAsync();

        if (_client == null)
            throw new InvalidOperationException("Supabase client not initialized");

        // Set competition date from metadata
        _competitionDate = metadata.Date;

        var competition = new LiveCompetition
        {
            CompetitionDate = metadata.Date,
            Name = metadata.Name,
            Date = metadata.Date,
            Location = metadata.Location,
            Organizer = metadata.Organizer,
            EventType = metadata.EventType,
            TerrainType = metadata.TerrainType,
            RaceNumber = metadata.RaceNumber,
            Courses = metadata.Courses?.Select(c => new SupabaseCourse
            {
                Name = c.Name,
                Level = c.Level,
                Length = (int)c.Length,
                Controls = c.Controls?.Select(ctrl => new SupabaseControl
                {
                    No = ctrl.No,
                    Code = ctrl.Code
                }).ToList() ?? new List<SupabaseControl>()
            }).ToList() ?? new List<SupabaseCourse>(),
            Status = "live"
        };

        try
        {
            await _client
                .From<LiveCompetition>()
                .OnConflict("competition_date")
                .Upsert(competition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload metadata to Supabase: {ex.Message}", ex);
        }
    }

    public async Task<int> WriteResultsAsync(IReadOnlyList<RaceResult> results, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (_client == null)
            throw new InvalidOperationException("Supabase client not initialized");

        if (string.IsNullOrEmpty(_competitionDate))
            throw new InvalidOperationException("Competition date not set. Call UploadMetadataAsync first.");

        var liveResults = results.Select(MapToLiveResult).ToList();

        try
        {
            await _client
                .From<LiveResult>()
                .OnConflict("id,competition_date")
                .Upsert(liveResults);

            // Upsert doesn't always return models in response, so return the count we attempted to write
            return liveResults.Count;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to write results to Supabase: {ex.Message}", ex);
        }
    }

    public async Task<int> GetRecordCountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (_client == null)
            throw new InvalidOperationException("Supabase client not initialized");

        try
        {
            var response = await _client
                .From<LiveResult>()
                .Where(x => x.CompetitionDate == _competitionDate)
                .Get();

            return response.Models.Count;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureInitializedAsync();

            if (_client == null)
                return false;

            var response = await _client
                .From<LiveResult>()
                .Limit(1)
                .Get();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> DeleteResultsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (_client == null)
            throw new InvalidOperationException("Supabase client not initialized");

        if (ids == null || ids.Count == 0)
            return 0;

        try
        {
            int deletedCount = 0;

            foreach (var id in ids)
            {
                await _client
                    .From<LiveResult>()
                    .Where(x => x.Id == id && x.CompetitionDate == _competitionDate)
                    .Delete();

                deletedCount++;
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete results from Supabase: {ex.Message}", ex);
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_client != null)
            return;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _client = new Client(_url, _apiKey, options);
        await _client.InitializeAsync();
    }

    private LiveResult MapToLiveResult(RaceResult result)
    {
        return new LiveResult
        {
            Id = result.Id,
            CompetitionDate = _competitionDate,
            Ecard = result.ECard,
            Ecard2 = result.ECard2 ?? string.Empty,
            FirstName = result.FirstName,
            LastName = result.LastName,
            Class = result.Class,
            Course = result.Course ?? string.Empty,
            Time = result.Time ?? string.Empty,
            Status = result.Status,
            StatusMessage = result.StatusMessage ?? string.Empty,
            Points = result.Points,
            TeamId = result.TeamId ?? string.Empty,
            TeamName = result.TeamName ?? string.Empty,
            SplitTimes = result.SplitTimes?.Select(st => new LiveResultSplitTime
            {
                Number = st.Number,
                Code = st.Code,
                Splittime = st.Splittime,
                Totaltime = st.Totaltime
            }).ToList(),
            FinishedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Supabase model for live_results table
/// Matches existing LiveResult from OBergen.LiveResults
/// IMPORTANT: Composite primary key (id, competition_date) to separate different races
/// </summary>
[Table("live_results")]
public class LiveResult : BaseModel
{
    [PrimaryKey("id", false)]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [PrimaryKey("competition_date", false)]
    [Column("competition_date")]
    [JsonProperty("competition_date")]
    public string CompetitionDate { get; set; } = string.Empty;

    [Column("eCard")]
    [JsonProperty("eCard")]
    public string Ecard { get; set; } = string.Empty;

    [Column("eCard2")]
    [JsonProperty("eCard2")]
    public string Ecard2 { get; set; } = string.Empty;

    [Column("firstName")]
    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [Column("lastName")]
    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [Column("time")]
    [JsonProperty("time")]
    public string Time { get; set; } = string.Empty;

    [Column("status")]
    [JsonProperty("status")]
    public string Status { get; set; } = "A";

    [Column("statusMessage")]
    [JsonProperty("statusMessage")]
    public string StatusMessage { get; set; } = string.Empty;

    [Column("class")]
    [JsonProperty("class")]
    public string Class { get; set; } = string.Empty;

    [Column("course")]
    [JsonProperty("course")]
    public string Course { get; set; } = string.Empty;

    [Column("points")]
    [JsonProperty("points")]
    public string Points { get; set; } = "0";

    [Column("teamId")]
    [JsonProperty("teamId")]
    public string TeamId { get; set; } = string.Empty;

    [Column("teamName")]
    [JsonProperty("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [Column("splitTimes")]
    [JsonProperty("splitTimes")]
    public List<LiveResultSplitTime>? SplitTimes { get; set; }

    [Column("finished_at")]
    [JsonProperty("finished_at")]
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Split time matching Supabase JSONB structure
/// </summary>
public class LiveResultSplitTime
{
    [JsonProperty("number")]
    public int Number { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("splittime")]
    public int Splittime { get; set; }

    [JsonProperty("totaltime")]
    public int Totaltime { get; set; }
}
